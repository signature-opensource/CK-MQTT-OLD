using CK.Core;
using CK.MQTT.Sdk.Packets;
using CK.MQTT.Sdk.Storage;
using System.Linq;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk.Flows
{
    internal class PublishReceiverFlow : PublishFlow
    {
        protected readonly IMqttTopicEvaluator topicEvaluator;
        protected readonly IRepository<RetainedMessage> retainedRepository;

        public PublishReceiverFlow( IMqttTopicEvaluator topicEvaluator,
            IRepository<RetainedMessage> retainedRepository,
            IRepository<ClientSession> sessionRepository,
            MqttConfiguration configuration )
            : base( sessionRepository, configuration )
        {
            this.topicEvaluator = topicEvaluator;
            this.retainedRepository = retainedRepository;
        }

        public override async Task ExecuteAsync( IActivityMonitor m, string clientId, IPacket input, IMqttChannel<IPacket> channel )
        {
            if( input.Type == MqttPacketType.Publish )
            {
                Publish publish = input as Publish;

                await HandlePublishAsync( m, clientId, publish, channel );
            }
            else if( input.Type == MqttPacketType.PublishRelease )
            {
                PublishRelease publishRelease = input as PublishRelease;

                await HandlePublishReleaseAsync( m, clientId, publishRelease, channel );
            }
        }

        protected virtual Task ProcessPublishAsync( IActivityMonitor m, Publish publish, string clientId )
        {
            return Task.CompletedTask;
        }

        protected virtual void Validate( Publish publish, string clientId )
        {
            if( publish.QualityOfService != MqttQualityOfService.AtMostOnce && !publish.PacketId.HasValue )
            {
                throw new MqttException( ClientProperties.PublishReceiverFlow_PacketIdRequired );
            }

            if( publish.QualityOfService == MqttQualityOfService.AtMostOnce && publish.PacketId.HasValue )
            {
                throw new MqttException( ClientProperties.PublishReceiverFlow_PacketIdNotAllowed );
            }
        }

        async Task HandlePublishAsync( IActivityMonitor m, string clientId, Publish publish, IMqttChannel<IPacket> channel )
        {
            Validate( publish, clientId );

            MqttQualityOfService qos = configuration.GetSupportedQos( publish.QualityOfService );
            ClientSession session = sessionRepository.Read( clientId );

            if( session == null )
            {
                throw new MqttException( ClientProperties.SessionRepository_ClientSessionNotFound( clientId ) );
            }

            if( qos == MqttQualityOfService.ExactlyOnce && session.GetPendingAcknowledgements().Any( ack => ack.Type == MqttPacketType.PublishReceived && ack.PacketId == publish.PacketId.Value ) )
            {
                await SendQosAck( m, clientId, qos, publish, channel );

                return;
            }

            await SendQosAck( m, clientId, qos, publish, channel );
            await ProcessPublishAsync( m, publish, clientId );
        }

        async Task HandlePublishReleaseAsync( IActivityMonitor m, string clientId, PublishRelease publishRelease, IMqttChannel<IPacket> channel )
        {
            RemovePendingAcknowledgement( clientId, publishRelease.PacketId, MqttPacketType.PublishReceived );

            await SendAckAsync( m, clientId, new PublishComplete( publishRelease.PacketId ), channel );
        }

        async Task SendQosAck( IActivityMonitor m, string clientId, MqttQualityOfService qos, Publish publish, IMqttChannel<IPacket> channel )
        {
            if( qos == MqttQualityOfService.AtMostOnce )
            {
                return;
            }
            else if( qos == MqttQualityOfService.AtLeastOnce )
            {
                await SendAckAsync( m, clientId, new PublishAck( publish.PacketId.Value ), channel );
            }
            else
            {
                await SendAckAsync( m, clientId, new PublishReceived( publish.PacketId.Value ), channel );
            }
        }
    }
}
