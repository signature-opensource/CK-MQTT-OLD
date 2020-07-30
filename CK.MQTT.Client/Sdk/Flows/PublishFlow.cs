using CK.Core;

using CK.MQTT.Sdk.Packets;
using CK.MQTT.Sdk.Storage;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk.Flows
{
    internal abstract class PublishFlow : IPublishFlow
    {
        protected readonly IRepository<ClientSession> sessionRepository;
        protected readonly MqttConfiguration configuration;

        protected PublishFlow( IRepository<ClientSession> sessionRepository,
            MqttConfiguration configuration )
        {
            this.sessionRepository = sessionRepository;
            this.configuration = configuration;
        }

        public abstract Task ExecuteAsync( IActivityMonitor m, string clientId, IPacket input, IMqttChannel<IPacket> channel );

        public async Task SendAckAsync( IActivityMonitor m, string clientId, IFlowPacket ack, IMqttChannel<IPacket> channel, PendingMessageStatus status = PendingMessageStatus.PendingToSend )
        {
            if( (ack.Type == MqttPacketType.PublishReceived || ack.Type == MqttPacketType.PublishRelease) &&
                status == PendingMessageStatus.PendingToSend )
            {
                SavePendingAcknowledgement( ack, clientId );
            }

            if( !channel.IsConnected )
            {
                return;
            }

            await channel.SendAsync( new Mon<IPacket>( m, ack ) );

            if( ack.Type == MqttPacketType.PublishReceived )
            {
                await MonitorAckAsync<PublishRelease>( m, ack, clientId, channel );
            }
            else if( ack.Type == MqttPacketType.PublishRelease )
            {
                await MonitorAckAsync<PublishComplete>( m, ack, clientId, channel );
            }
        }

        protected void RemovePendingAcknowledgement( string clientId, ushort packetId, MqttPacketType type )
        {
            ClientSession session = sessionRepository.Read( clientId );

            if( session == null )
            {
                throw new MqttException( ClientProperties.SessionRepository_ClientSessionNotFound( clientId ) );
            }

            PendingAcknowledgement pendingAcknowledgement = session
                .GetPendingAcknowledgements()
                .FirstOrDefault( u => u.Type == type && u.PacketId == packetId );

            session.RemovePendingAcknowledgement( pendingAcknowledgement );

            sessionRepository.Update( session );
        }

        protected async Task MonitorAckAsync<T>( IActivityMonitor m, IFlowPacket sentMessage, string clientId, IMqttChannel<IPacket> channel )
            where T : IFlowPacket
        {
            using( IDisposable intervalSubscription = Observable
                .Interval( TimeSpan.FromSeconds( configuration.WaitTimeoutSecs ), NewThreadScheduler.Default )
                .Subscribe( async _ =>
                {
                    if( channel.IsConnected )
                    {
                        m.Warn( ClientProperties.PublishFlow_RetryingQoSFlow( sentMessage.Type, clientId ) );

                        await channel.SendAsync( new Mon<IPacket>( m, sentMessage ) );
                    }
                } ) )
            {
                await channel
                    .ReceiverStream
                    .OfMonitoredType<T, IPacket>()
                    .FirstOrDefaultAsync( x => x.Item.PacketId == sentMessage.PacketId );
            }
        }

        void SavePendingAcknowledgement( IFlowPacket ack, string clientId )
        {
            if( ack.Type != MqttPacketType.PublishReceived && ack.Type != MqttPacketType.PublishRelease )
            {
                return;
            }

            PendingAcknowledgement unacknowledgeMessage = new PendingAcknowledgement
            {
                PacketId = ack.PacketId,
                Type = ack.Type
            };

            ClientSession session = sessionRepository.Read( clientId );

            if( session == null )
            {
                throw new MqttException( ClientProperties.SessionRepository_ClientSessionNotFound( clientId ) );
            }

            session.AddPendingAcknowledgement( unacknowledgeMessage );

            sessionRepository.Update( session );
        }
    }
}
