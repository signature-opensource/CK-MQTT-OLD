using CK.MQTT.Sdk.Packets;
using CK.MQTT.Sdk.Storage;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk.Flows
{
    internal class ServerConnectFlow : IProtocolFlow
    {
        static readonly ITracer _tracer = Tracer.Get<ServerConnectFlow>();

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

        public async Task ExecuteAsync( string clientId, IPacket input, IMqttChannel<IPacket> channel )
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

            if( connect.CleanSession && session != null )
            {
                _sessionRepository.Delete( session.Id );
                session = null;

                _tracer.Info( ServerProperties.Server_CleanedOldSession( clientId ) );
            }

            bool sendPendingMessages = false;

            if( session == null )
            {
                session = new ClientSession( clientId, connect.CleanSession );

                _sessionRepository.Create( session );

                _tracer.Info( ServerProperties.Server_CreatedSession( clientId ) );
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

            await channel.SendAsync( new ConnectAck( MqttConnectionStatus.Accepted, sessionPresent ) );

            if( sendPendingMessages )
            {
                await SendPendingMessagesAsync( session, channel );
                await SendPendingAcknowledgementsAsync( session, channel );
            }
        }

        async Task SendPendingMessagesAsync( ClientSession session, IMqttChannel<IPacket> channel )
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

                    await _senderFlow.SendPublishAsync( session.Id, publish, channel );
                }
                else
                {
                    await _senderFlow.SendPublishAsync( session.Id, publish, channel, PendingMessageStatus.PendingToAcknowledge );
                }
            }
        }

        async Task SendPendingAcknowledgementsAsync( ClientSession session, IMqttChannel<IPacket> channel )
        {
            foreach( PendingAcknowledgement pendingAcknowledgement in session.GetPendingAcknowledgements() )
            {
                IFlowPacket ack = default;

                if( pendingAcknowledgement.Type == MqttPacketType.PublishReceived )
                    ack = new PublishReceived( pendingAcknowledgement.PacketId );
                else if( pendingAcknowledgement.Type == MqttPacketType.PublishRelease )
                    ack = new PublishRelease( pendingAcknowledgement.PacketId );

                await _senderFlow.SendAckAsync( session.Id, ack, channel, PendingMessageStatus.PendingToAcknowledge );
            }
        }
    }
}
