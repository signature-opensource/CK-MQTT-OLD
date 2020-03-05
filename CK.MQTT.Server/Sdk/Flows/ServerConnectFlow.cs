using CK.Core;

using CK.MQTT.Sdk.Packets;
using CK.MQTT.Sdk.Storage;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk.Flows
{
    internal class ServerConnectFlow : IProtocolFlow
    {
        readonly IMqttAuthenticationProvider _authenticationProvider;
        readonly IRepository<ClientSession> _sessionRepository;
        readonly IRepository<ConnectionWill> _willRepository;
        readonly IPublishSenderFlow _senderFlow;

        public ServerConnectFlow( IMqttAuthenticationProvider authenticationProvider,
            IRepository<ClientSession> sessionRepository,
            IRepository<ConnectionWill> willRepository,
            IPublishSenderFlow senderFlow )
        {
            _authenticationProvider = authenticationProvider;
            _sessionRepository = sessionRepository;
            _willRepository = willRepository;
            _senderFlow = senderFlow;
        }

        public async Task ExecuteAsync( IActivityMonitor m, string clientId, IPacket input, IMqttChannel<IPacket> channel )
        {
            if( input.Type != MqttPacketType.Connect )
                return;

            Connect connect = input as Connect;

            if( !_authenticationProvider.Authenticate( clientId, connect.UserName, connect.Password ) )
            {
                throw new MqttConnectionException( MqttConnectionStatus.BadUserNameOrPassword );
            }

            ClientSession session = _sessionRepository.Read( clientId );
            bool sessionPresent = connect.CleanSession ? false : session != null;

            m.Info( $"Client connecting with protocol level {connect.ProtocolLelvel}." );

            if( connect.CleanSession && session != null )
            {
                _sessionRepository.Delete( session.Id );
                session = null;

                m.Info( ServerProperties.Server_CleanedOldSession( clientId ) );
            }

            bool sendPendingMessages = false;

            if( session == null )
            {
                session = new ClientSession( clientId, connect.CleanSession );

                _sessionRepository.Create( session );

                m.Info( ServerProperties.Server_CreatedSession( clientId ) );
            }
            else
            {
                sendPendingMessages = true;
            }

            if( connect.Will != null )
            {
                ConnectionWill connectionWill = new ConnectionWill( clientId, connect.Will );

                _willRepository.Create( connectionWill );
            }

            await channel.SendAsync( new Mon<IPacket>( m, new ConnectAck( MqttConnectionStatus.Accepted, sessionPresent ) ) );

            if( sendPendingMessages )
            {
                await SendPendingMessagesAsync(m, session, channel );
                await SendPendingAcknowledgementsAsync(m, session, channel );
            }
        }

        async Task SendPendingMessagesAsync( IActivityMonitor m, ClientSession session, IMqttChannel<IPacket> channel )
        {
            foreach( PendingMessage pendingMessage in session.GetPendingMessages() )
            {
                Publish publish = new Publish( pendingMessage.Topic, pendingMessage.QualityOfService,
                    pendingMessage.Retain, pendingMessage.Duplicated, pendingMessage.PacketId )
                {
                    Payload = pendingMessage.Payload
                };

                if( pendingMessage.Status == PendingMessageStatus.PendingToSend )
                {
                    session.RemovePendingMessage( pendingMessage );
                    _sessionRepository.Update( session );

                    await _senderFlow.SendPublishAsync( m, session.Id, publish, channel );
                }
                else
                {
                    await _senderFlow.SendPublishAsync(m, session.Id, publish, channel, PendingMessageStatus.PendingToAcknowledge );
                }
            }
        }

        async Task SendPendingAcknowledgementsAsync( IActivityMonitor m, ClientSession session, IMqttChannel<IPacket> channel )
        {
            foreach( PendingAcknowledgement pendingAcknowledgement in session.GetPendingAcknowledgements() )
            {
                IFlowPacket ack = default;

                if( pendingAcknowledgement.Type == MqttPacketType.PublishReceived )
                    ack = new PublishReceived( pendingAcknowledgement.PacketId );
                else if( pendingAcknowledgement.Type == MqttPacketType.PublishRelease )
                    ack = new PublishRelease( pendingAcknowledgement.PacketId );

                await _senderFlow.SendAckAsync( m, session.Id, ack, channel, PendingMessageStatus.PendingToAcknowledge );
            }
        }
    }
}
