using CK.MQTT.Sdk.Packets;
using CK.MQTT.Sdk.Storage;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk.Flows
{
    internal class ClientConnectFlow : IProtocolFlow
    {
        readonly IRepository<ClientSession> _sessionRepository;
        readonly IPublishSenderFlow _senderFlow;

        public ClientConnectFlow( IRepository<ClientSession> sessionRepository,
            IPublishSenderFlow senderFlow )
        {
            _sessionRepository = sessionRepository;
            _senderFlow = senderFlow;
        }

        public async Task ExecuteAsync( string clientId, IPacket input, IMqttChannel<IPacket> channel )
        {
            if( input.Type != MqttPacketType.ConnectAck )
            {
                return;
            }

            ConnectAck ack = input as ConnectAck;

            if( ack.Status != MqttConnectionStatus.Accepted )
            {
                return;
            }

            ClientSession session = _sessionRepository.Read( clientId );

            if( session == null )
            {
                throw new MqttException( string.Format( Properties.SessionRepository_ClientSessionNotFound, clientId ) );
            }

            await SendPendingMessagesAsync( session, channel );
            await SendPendingAcknowledgementsAsync( session, channel );
        }

        async Task SendPendingMessagesAsync( ClientSession session, IMqttChannel<IPacket> channel )
        {
            foreach( PendingMessage pendingMessage in session.GetPendingMessages() )
            {
                Publish publish = new Publish( pendingMessage.Topic, pendingMessage.QualityOfService,
                    pendingMessage.Retain, pendingMessage.Duplicated, pendingMessage.PacketId );

                await _senderFlow
                    .SendPublishAsync( session.Id, publish, channel, PendingMessageStatus.PendingToAcknowledge );
            }
        }

        async Task SendPendingAcknowledgementsAsync( ClientSession session, IMqttChannel<IPacket> channel )
        {
            foreach( PendingAcknowledgement pendingAcknowledgement in session.GetPendingAcknowledgements() )
            {
                IFlowPacket ack = default;

                if( pendingAcknowledgement.Type == MqttPacketType.PublishReceived )
                {
                    ack = new PublishReceived( pendingAcknowledgement.PacketId );
                }
                else if( pendingAcknowledgement.Type == MqttPacketType.PublishRelease )
                {
                    ack = new PublishRelease( pendingAcknowledgement.PacketId );
                }

                await _senderFlow.SendAckAsync( session.Id, ack, channel );
            }
        }
    }
}
