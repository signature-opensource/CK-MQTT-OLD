using CK.Core;
using CK.MQTT.Sdk.Packets;
using CK.MQTT.Sdk.Storage;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk.Flows
{
    internal class ServerPublishReceiverFlow : PublishReceiverFlow, IServerPublishReceiverFlow
    {
        readonly IConnectionProvider _connectionProvider;
        readonly IPublishSenderFlow _senderFlow;
        readonly IRepository<ConnectionWill> _willRepository;
        readonly IPacketIdProvider _packetIdProvider;
        readonly ISubject<MqttUndeliveredMessage> _undeliveredMessagesListener;

        public ServerPublishReceiverFlow( IMqttTopicEvaluator topicEvaluator,
            IConnectionProvider connectionProvider,
            IPublishSenderFlow senderFlow,
            IRepository<RetainedMessage> retainedRepository,
            IRepository<ClientSession> sessionRepository,
            IRepository<ConnectionWill> willRepository,
            IPacketIdProvider packetIdProvider,
            ISubject<MqttUndeliveredMessage> undeliveredMessagesListener,
            MqttConfiguration configuration )
            : base( topicEvaluator, retainedRepository, sessionRepository, configuration )
        {
            _connectionProvider = connectionProvider;
            _senderFlow = senderFlow;
            _willRepository = willRepository;
            _packetIdProvider = packetIdProvider;
            _undeliveredMessagesListener = undeliveredMessagesListener;
        }

        public async Task SendWillAsync( IActivityMonitor m, string clientId )
        {
            ConnectionWill will = _willRepository.Read( clientId );

            if( will != null && will.Will != null )
            {
                Publish willPublish = new Publish( will.Will.Topic, will.Will.Payload, will.Will.QualityOfService, will.Will.Retain, duplicated: false );

                m.Info( $"Server - Sending last will message of client {clientId} to topic {willPublish.Topic}" );

                await DispatchAsync( m, willPublish, clientId, isWill: true );
            }
        }

        protected override async Task ProcessPublishAsync( IActivityMonitor m, Publish publish, string clientId )
        {
            if( publish.Retain )
            {
                RetainedMessage existingRetainedMessage = retainedRepository.Read( publish.Topic );

                if( existingRetainedMessage != null )
                {
                    retainedRepository.Delete( existingRetainedMessage.Id );
                }

                if( publish.Payload.Length > 0 )
                {
                    RetainedMessage retainedMessage = new RetainedMessage( publish.Topic,
                        publish.QualityOfService,
                        publish.Payload.ToArray() );//Spanify this.

                    retainedRepository.Create( retainedMessage );
                }
            }

            await DispatchAsync( m, publish, clientId );
        }

        protected override void Validate( Publish publish, string clientId )
        {
            base.Validate( publish, clientId );

            if( publish.Topic.Trim().StartsWith( "$" ) && !_connectionProvider.PrivateClients.Contains( clientId ) )
            {
                throw new MqttException( ServerProperties.ServerPublishReceiverFlow_SystemMessageNotAllowedForClient );
            }
        }

        async Task DispatchAsync( IActivityMonitor m, Publish publish, string clientId, bool isWill = false )
        {
            System.Collections.Generic.IEnumerable<ClientSubscription> subscriptions = sessionRepository
                .ReadAll().ToList()
                .SelectMany( s => s.GetSubscriptions() )
                .Where( x => topicEvaluator.Matches( publish.Topic, x.TopicFilter ) );

            if( !subscriptions.Any() )
            {
                m.Info( ServerProperties.ServerPublishReceiverFlow_TopicNotSubscribed( publish.Topic, clientId ) );

                _undeliveredMessagesListener.OnNext( new MqttUndeliveredMessage { SenderId = clientId, Message = new MqttApplicationMessage( publish.Topic, publish.Payload ) } );
            }
            else
            {
                foreach( ClientSubscription subscription in subscriptions )
                {
                    await DispatchAsync( m, publish, subscription, isWill );
                }
            }
        }

        async Task DispatchAsync( IActivityMonitor m, Publish publish, ClientSubscription subscription, bool isWill = false )
        {
            MqttQualityOfService requestedQos = isWill ? publish.QualityOfService : subscription.MaximumQualityOfService;
            MqttQualityOfService supportedQos = configuration.GetSupportedQos( requestedQos );
            bool retain = isWill ? publish.Retain : false;
            ushort? packetId = supportedQos == MqttQualityOfService.AtMostOnce ? null : (ushort?)_packetIdProvider.GetPacketId();
            Publish subscriptionPublish = new Publish( publish.Topic, publish.Payload, supportedQos, retain, duplicated: false, packetId: packetId );
            IMqttChannel<IPacket> clientChannel = _connectionProvider.GetConnection( m, subscription.ClientId );

            await _senderFlow.SendPublishAsync( m, subscription.ClientId, subscriptionPublish, clientChannel );
        }
    }
}
