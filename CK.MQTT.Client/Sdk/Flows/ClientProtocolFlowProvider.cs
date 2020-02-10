using CK.MQTT.Sdk.Packets;
using CK.MQTT.Sdk.Storage;
using System.Collections.Generic;

namespace CK.MQTT.Sdk.Flows
{
    internal class ClientProtocolFlowProvider : ProtocolFlowProvider
    {
        public ClientProtocolFlowProvider( IMqttTopicEvaluator topicEvaluator,
            IRepositoryProvider repositoryProvider,
            MqttConfiguration configuration )
            : base( topicEvaluator, repositoryProvider, configuration )
        {
        }

        protected override IDictionary<ProtocolFlowType, IProtocolFlow> InitializeFlows()
        {
            Dictionary<ProtocolFlowType, IProtocolFlow> flows = new Dictionary<ProtocolFlowType, IProtocolFlow>();

            IRepository<ClientSession> sessionRepository = repositoryProvider.GetRepository<ClientSession>();
            IRepository<RetainedMessage> retainedRepository = repositoryProvider.GetRepository<RetainedMessage>();
            PublishSenderFlow senderFlow = new PublishSenderFlow( sessionRepository, configuration );

            flows.Add( ProtocolFlowType.Connect, new ClientConnectFlow( sessionRepository, senderFlow ) );
            flows.Add( ProtocolFlowType.PublishSender, senderFlow );
            flows.Add( ProtocolFlowType.PublishReceiver, new PublishReceiverFlow( topicEvaluator,
                retainedRepository, sessionRepository, configuration ) );
            flows.Add( ProtocolFlowType.Subscribe, new ClientSubscribeFlow() );
            flows.Add( ProtocolFlowType.Unsubscribe, new ClientUnsubscribeFlow() );
            flows.Add( ProtocolFlowType.Ping, new PingFlow() );

            return flows;
        }

        protected override bool IsValidPacketType( MqttPacketType packetType )
        {
            return packetType == MqttPacketType.ConnectAck ||
                packetType == MqttPacketType.SubscribeAck ||
                packetType == MqttPacketType.UnsubscribeAck ||
                packetType == MqttPacketType.Publish ||
                packetType == MqttPacketType.PublishAck ||
                packetType == MqttPacketType.PublishComplete ||
                packetType == MqttPacketType.PublishReceived ||
                packetType == MqttPacketType.PublishRelease ||
                packetType == MqttPacketType.PingResponse;
        }
    }
}
