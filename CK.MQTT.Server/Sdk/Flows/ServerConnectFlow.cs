using System.Diagnostics;
using CK.MQTT.Sdk.Packets;
using CK.MQTT.Sdk.Storage;
using System.Threading.Tasks;
using CK.Core;

namespace CK.MQTT.Sdk.Flows
{
    internal class ServerConnectFlow : IProtocolFlow
    {
        readonly IMqttAuthenticationProvider authenticationProvider;
        readonly IRepository<ClientSession> sessionRepository;
        readonly IRepository<ConnectionWill> willRepository;
        readonly IPublishSenderFlow senderFlow;

        public ServerConnectFlow( IMqttAuthenticationProvider authenticationProvider,
            IRepository<ClientSession> sessionRepository,
            IRepository<ConnectionWill> willRepository,
            IPublishSenderFlow senderFlow )
        {
            this.authenticationProvider = authenticationProvider;
            this.sessionRepository = sessionRepository;
            this.willRepository = willRepository;
            this.senderFlow = senderFlow;
        }

        public async Task ExecuteAsync( IActivityMonitor m, string clientId, IPacket input, IMqttChannel<IPacket> channel )
        {
            if( input.Type != MqttPacketType.Connect )
                return;

            var connect = input as Connect;

            if( !authenticationProvider.Authenticate( clientId, connect.UserName, connect.Password ) )
            {
                throw new MqttConnectionException( MqttConnectionStatus.BadUserNameOrPassword );
            }

            var session = sessionRepository.Read( clientId );
            var sessionPresent = connect.CleanSession ? false : session != null;

            if( connect.CleanSession && session != null )
            {
                sessionRepository.Delete( session.Id );
                session = null;

                m.Info( string.Format( ServerProperties.Server_CleanedOldSession, clientId ) );
            }

            var sendPendingMessages = false;

            if( session == null )
            {
                session = new ClientSession( clientId, connect.CleanSession );

                sessionRepository.Create( session );
                m.Info( string.Format( ServerProperties.Server_CreatedSession, clientId ) );
            }
            else
            {
                sendPendingMessages = true;
            }

            if( connect.Will != null )
            {
                var connectionWill = new ConnectionWill( clientId, connect.Will );

                willRepository.Create( connectionWill );
            }

            await channel.SendAsync( m, new ConnectAck( MqttConnectionStatus.Accepted, sessionPresent ) )
                .ConfigureAwait( continueOnCapturedContext: false );

            if( sendPendingMessages )
            {
                await SendPendingMessagesAsync( m, session, channel )
                    .ConfigureAwait( continueOnCapturedContext: false );
                await SendPendingAcknowledgementsAsync( m, session, channel )
                    .ConfigureAwait( continueOnCapturedContext: false );
            }
        }

        async Task SendPendingMessagesAsync( IActivityMonitor m, ClientSession session, IMqttChannel<IPacket> channel )
        {
            foreach( var pendingMessage in session.GetPendingMessages() )
            {
                var publish = new Publish( pendingMessage.Topic, pendingMessage.QualityOfService,
                    pendingMessage.Retain, pendingMessage.Duplicated, pendingMessage.PacketId )
                {
                    Payload = pendingMessage.Payload
                };

                if( pendingMessage.Status == PendingMessageStatus.PendingToSend )
                {
                    session.RemovePendingMessage( pendingMessage );
                    sessionRepository.Update( session );

                    await senderFlow.SendPublishAsync( m, session.Id, publish, channel )
                        .ConfigureAwait( continueOnCapturedContext: false );
                }
                else
                {
                    await senderFlow.SendPublishAsync( m, session.Id, publish, channel, PendingMessageStatus.PendingToAcknowledge )
                        .ConfigureAwait( continueOnCapturedContext: false );
                }
            }
        }

        async Task SendPendingAcknowledgementsAsync( IActivityMonitor m, ClientSession session, IMqttChannel<IPacket> channel )
        {
            foreach( var pendingAcknowledgement in session.GetPendingAcknowledgements() )
            {
                IFlowPacket ack = default;

                if( pendingAcknowledgement.Type == MqttPacketType.PublishReceived )
                    ack = new PublishReceived( pendingAcknowledgement.PacketId );
                else if( pendingAcknowledgement.Type == MqttPacketType.PublishRelease )
                    ack = new PublishRelease( pendingAcknowledgement.PacketId );

                await senderFlow.SendAckAsync( m, session.Id, ack, channel, PendingMessageStatus.PendingToAcknowledge )
                    .ConfigureAwait( continueOnCapturedContext: false );
            }
        }
    }
}
