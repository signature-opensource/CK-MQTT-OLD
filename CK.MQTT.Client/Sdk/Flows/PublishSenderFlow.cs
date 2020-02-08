using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CK.MQTT.Sdk.Packets;
using CK.MQTT.Sdk.Storage;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System;
using CK.Core;

namespace CK.MQTT.Sdk.Flows
{
    internal class PublishSenderFlow : PublishFlow, IPublishSenderFlow
    {
        IDictionary<MqttPacketType, Func<string, ushort, IFlowPacket>> _senderRules;

        public PublishSenderFlow( IRepository<ClientSession> sessionRepository,
            MqttConfiguration configuration )
            : base( sessionRepository, configuration )
        {
            DefineSenderRules();
        }

        public override async Task ExecuteAsync( IActivityMonitor m, string clientId, IPacket input, IMqttChannel<IPacket> channel )
        {
            if( !_senderRules.TryGetValue( input.Type, out Func<string, ushort, IFlowPacket> senderRule ) )
            {
                return;
            }

            if( !(input is IFlowPacket flowPacket) )
            {
                return;
            }

            var ackPacket = senderRule( clientId, flowPacket.PacketId );

            if( ackPacket != default( IFlowPacket ) )
            {
                await SendAckAsync( m, clientId, ackPacket, channel )
                    .ConfigureAwait( continueOnCapturedContext: false );
            }
        }

        public async Task SendPublishAsync( IActivityMonitor m, string clientId, Publish message, IMqttChannel<IPacket> channel, PendingMessageStatus status = PendingMessageStatus.PendingToSend )
        {
            if( channel == null || !channel.IsConnected )
            {
                SaveMessage( message, clientId, PendingMessageStatus.PendingToSend );
                return;
            }

            var qos = configuration.GetSupportedQos( message.QualityOfService );

            if( qos != MqttQualityOfService.AtMostOnce && status == PendingMessageStatus.PendingToSend )
            {
                SaveMessage( message, clientId, PendingMessageStatus.PendingToAcknowledge );
            }

            await channel.SendAsync( m, message )
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
            var session = sessionRepository.Read( clientId );

            if( session == null )
            {
                throw new MqttException( string.Format( Properties.SessionRepository_ClientSessionNotFound, clientId ) );
            }

            var pendingMessage = session
                .GetPendingMessages()
                .FirstOrDefault( p => p.PacketId.HasValue && p.PacketId.Value == packetId );

            session.RemovePendingMessage( pendingMessage );

            sessionRepository.Update( session );
        }

        protected async Task MonitorAckAsync<T>( Publish sentMessage, string clientId, IMqttChannel<IPacket> channel )
            where T : IFlowPacket
        {
            var m = new ActivityMonitor();
            using( var intervalSubscription = Observable
                .Interval( TimeSpan.FromSeconds( configuration.WaitTimeoutSecs ), NewThreadScheduler.Default )
                .Subscribe( async _ =>
                {
                    if( channel.IsConnected )
                    {
                        m.Warn( string.Format( Properties.PublishFlow_RetryingQoSFlow, sentMessage.Type, clientId ) );

                        var duplicated = new Publish( sentMessage.Topic, sentMessage.QualityOfService,
                            sentMessage.Retain, duplicated: true, packetId: sentMessage.PacketId )
                        {
                            Payload = sentMessage.Payload
                        };

                        await channel.SendAsync(m, message: duplicated );
                    }
                } ) )
            {

                await channel
                    .ReceiverStream
                    .ObserveOn( NewThreadScheduler.Default )
                    .OfType<T>()
                    .FirstOrDefaultAsync( x => x.PacketId == sentMessage.PacketId.Value );

            }
        }

        void DefineSenderRules()
        {
            _senderRules = new Dictionary<MqttPacketType, Func<string, ushort, IFlowPacket>>();

            _senderRules.Add( MqttPacketType.PublishAck, ( clientId, packetId ) =>
            {
                RemovePendingMessage( clientId, packetId );

                return default( IFlowPacket );
            } );

            _senderRules.Add( MqttPacketType.PublishReceived, ( clientId, packetId ) =>
            {
                RemovePendingMessage( clientId, packetId );

                return new PublishRelease( packetId );
            } );

            _senderRules.Add( MqttPacketType.PublishComplete, ( clientId, packetId ) =>
            {
                RemovePendingAcknowledgement( clientId, packetId, MqttPacketType.PublishRelease );

                return default( IFlowPacket );
            } );
        }

        void SaveMessage( Publish message, string clientId, PendingMessageStatus status )
        {
            if( message.QualityOfService == MqttQualityOfService.AtMostOnce )
            {
                return;
            }

            var session = sessionRepository.Read( clientId );

            if( session == null )
            {
                throw new MqttException( string.Format( Properties.SessionRepository_ClientSessionNotFound, clientId ) );
            }

            var savedMessage = new PendingMessage
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
