using CK.MQTT.Sdk.Packets;
using CK.MQTT.Sdk.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk.Flows
{
    internal class PublishSenderFlow : PublishFlow, IPublishSenderFlow
    {
        static readonly ITracer _tracer = Tracer.Get<PublishSenderFlow>();

        IDictionary<MqttPacketType, Func<string, ushort, IFlowPacket>> _senderRules;

        public PublishSenderFlow( IRepository<ClientSession> sessionRepository,
            MqttConfiguration configuration )
            : base( sessionRepository, configuration )
        {
            DefineSenderRules();
        }

        public override async Task ExecuteAsync( string clientId, IPacket input, IMqttChannel<IPacket> channel )
        {
            if( !_senderRules.TryGetValue( input.Type, out Func<string, ushort, IFlowPacket> senderRule ) )
            {
                return;
            }

            if( !(input is IFlowPacket flowPacket) ) return;

            IFlowPacket ackPacket = senderRule( clientId, flowPacket.PacketId );

            if( ackPacket != default( IFlowPacket ) )
            {
                await SendAckAsync( clientId, ackPacket, channel )
                    .ConfigureAwait( continueOnCapturedContext: false );
            }
        }

        public async Task SendPublishAsync( string clientId, Publish message, IMqttChannel<IPacket> channel, PendingMessageStatus status = PendingMessageStatus.PendingToSend )
        {
            if( channel == null || !channel.IsConnected )
            {
                SaveMessage( message, clientId, PendingMessageStatus.PendingToSend );
                return;
            }

            MqttQualityOfService qos = configuration.GetSupportedQos( message.QualityOfService );

            if( qos != MqttQualityOfService.AtMostOnce && status == PendingMessageStatus.PendingToSend )
            {
                SaveMessage( message, clientId, PendingMessageStatus.PendingToAcknowledge );
            }

            await channel.SendAsync( message )
                .ConfigureAwait( continueOnCapturedContext: false );

            if( qos == MqttQualityOfService.AtLeastOnce )
            {
                await MonitorAckAsync<PublishAck>( message, clientId, channel )
                    .ConfigureAwait( continueOnCapturedContext: false );
            }
            else if( qos == MqttQualityOfService.ExactlyOnce )
            {
                await MonitorAckAsync<PublishReceived>( message, clientId, channel ).ConfigureAwait( continueOnCapturedContext: false );
                await channel
                    .ReceiverStream
                    .ObserveOn( NewThreadScheduler.Default )
                    .OfType<PublishComplete>()
                    .FirstOrDefaultAsync( x => x.PacketId == message.PacketId.Value );
            }
        }

        protected void RemovePendingMessage( string clientId, ushort packetId )
        {
            ClientSession session = sessionRepository.Read( clientId );

            if( session == null )
            {
                throw new MqttException( string.Format( Properties.Resources.GetString( "SessionRepository_ClientSessionNotFound" ), clientId ) );
            }

            PendingMessage pendingMessage = session
                .GetPendingMessages()
                .FirstOrDefault( p => p.PacketId.HasValue && p.PacketId.Value == packetId );

            session.RemovePendingMessage( pendingMessage );

            sessionRepository.Update( session );
        }

        protected async Task MonitorAckAsync<T>( Publish sentMessage, string clientId, IMqttChannel<IPacket> channel )
            where T : IFlowPacket
        {
            IDisposable intervalSubscription = Observable
                .Interval( TimeSpan.FromSeconds( configuration.WaitTimeoutSecs ), NewThreadScheduler.Default )
                .Subscribe( async _ =>
                {
                    if( channel.IsConnected )
                    {
                        _tracer.Warn( Properties.Resources.GetString( "PublishFlow_RetryingQoSFlow" ), sentMessage.Type, clientId );

                        Publish duplicated = new Publish( sentMessage.Topic, sentMessage.QualityOfService,
                            sentMessage.Retain, duplicated: true, packetId: sentMessage.PacketId )
                        {
                            Payload = sentMessage.Payload
                        };

                        await channel.SendAsync( duplicated );
                    }
                } );

            await channel
                .ReceiverStream
                .ObserveOn( NewThreadScheduler.Default )
                .OfType<T>()
                .FirstOrDefaultAsync( x => x.PacketId == sentMessage.PacketId.Value );

            intervalSubscription.Dispose();
        }

        void DefineSenderRules()
        {
            _senderRules = new Dictionary<MqttPacketType, Func<string, ushort, IFlowPacket>>
            {
                {
                    MqttPacketType.PublishAck,
                    ( clientId, packetId ) =>
                    {
                        RemovePendingMessage( clientId, packetId );
                        return default;
                    }
                },
                {
                    MqttPacketType.PublishReceived,
                    ( clientId, packetId ) =>
                    {
                        RemovePendingMessage( clientId, packetId );
                        return new PublishRelease( packetId );
                    }
                },
                {
                    MqttPacketType.PublishComplete,
                    ( clientId, packetId ) =>
                    {
                        RemovePendingAcknowledgement( clientId, packetId, MqttPacketType.PublishRelease );
                        return default;
                    }
                }
            };
        }

        void SaveMessage( Publish message, string clientId, PendingMessageStatus status )
        {
            if( message.QualityOfService == MqttQualityOfService.AtMostOnce ) return;

            ClientSession session = sessionRepository.Read( clientId );

            if( session == null )
            {
                throw new MqttException( string.Format( Properties.Resources.GetString( "SessionRepository_ClientSessionNotFound" ), clientId ) );
            }

            PendingMessage savedMessage = new PendingMessage
            {
                Status = status,
                QualityOfService = message.QualityOfService,
                Duplicated = message.Duplicated,
                Retain = message.Retain,
                Topic = message.Topic,
                PacketId = message.PacketId,
                Payload = message.Payload
            };

            session.AddPendingMessage( savedMessage );

            sessionRepository.Update( session );
        }
    }
}
