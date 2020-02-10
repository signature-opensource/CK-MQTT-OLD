using CK.MQTT.Sdk.Packets;
using CK.MQTT.Sdk.Storage;
using System.Collections.Generic;
using System.Reactive.Subjects;

namespace CK.MQTT.Sdk.Flows
{
    internal class ServerProtocolFlowProvider : ProtocolFlowProvider
    {
        readonly IMqttAuthenticationProvider _authenticationProvider;
        readonly IConnectionProvider _connectionProvider;
        readonly IPacketIdProvider _packetIdProvider;
        readonly ISubject<MqttUndeliveredMessage> _undeliveredMessagesListener;

        public ServerProtocolFlowProvider( IMqttAuthenticationProvider authenticationProvider,
            IConnectionProvider connectionProvider,
            IMqttTopicEvaluator topicEvaluator,
            IRepositoryProvider repositoryProvider,
            IPacketIdProvider packetIdProvider,
            ISubject<MqttUndeliveredMessage> undeliveredMessagesListener,
            MqttConfiguration configuration )
            : base( topicEvaluator, repositoryProvider, configuration )
        {
            _authenticationProvider = authenticationProvider;
            _connectionProvider = connectionProvider;
            _packetIdProvider = packetIdProvider;
            _undeliveredMessagesListener = undeliveredMessagesListener;
        }

        protected override IDictionary<ProtocolFlowType, IProtocolFlow> InitializeFlows()
        {
            Dictionary<ProtocolFlowType, IProtocolFlow> flows = new Dictionary<ProtocolFlowType, IProtocolFlow>();

            IRepository<ClientSession> sessionRepository = repositoryProvider.GetRepository<ClientSession>();
            IRepository<ConnectionWill> willRepository = repositoryProvider.GetRepository<ConnectionWill>();
            IRepository<RetainedMessage> retainedRepository = repositoryProvider.GetRepository<RetainedMessage>();
            PublishSenderFlow senderFlow = new PublishSenderFlow( sessionRepository, configuration );

            flows.Add( ProtocolFlowType.Connect, new ServerConnectFlow( _authenticationProvider, sessionRepository, willRepository, senderFlow ) );
            flows.Add( ProtocolFlowType.PublishSender, senderFlow );
            flows.Add( ProtocolFlowType.PublishReceiver, new ServerPublishReceiverFlow( topicEvaluator, _connectionProvider,
                senderFlow, retainedRepository, sessionRepository, willRepository, _packetIdProvider, _undeliveredMessagesListener, configuration ) );
            flows.Add( ProtocolFlowType.Subscribe, new ServerSubscribeFlow( topicEvaluator, sessionRepository,
                retainedRepository, _packetIdProvider, senderFlow, configuration ) );
            flows.Add( ProtocolFlowType.Unsubscribe, new ServerUnsubscribeFlow( sessionRepository ) );
            flows.Add( ProtocolFlowType.Ping, new PingFlow() );
            flows.Add( ProtocolFlowType.Disconnect, new DisconnectFlow( _connectionProvider, sessionRepository, willRepository ) );

            return flows;
        }

        protected override bool IsValidPacketType( MqttPacketType packetType )
        {
            return packetType == MqttPacketType.Connect ||
                packetType == MqttPacketType.Subscribe ||
                packetType == MqttPacketType.Unsubscribe ||
                packetType == MqttPacketType.Publish ||
                packetType == MqttPacketType.PublishAck ||
                packetType == MqttPacketType.PublishComplete ||
                packetType == MqttPacketType.PublishReceived ||
                packetType == MqttPacketType.PublishRelease ||
                packetType == MqttPacketType.PingRequest ||
                packetType == MqttPacketType.Disconnect;
        }
    }
}
