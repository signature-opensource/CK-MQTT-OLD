using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using CK.MQTT;
using CK.MQTT.Sdk;
using CK.MQTT.Sdk.Flows;
using CK.MQTT.Sdk.Packets;
using CK.MQTT.Sdk.Storage;
using Moq;

namespace IntegrationTests
{
    internal class TestServerFlow : ServerProtocolFlowProvider
    {
        public TestServerFlow( IMqttAuthenticationProvider authenticationProvider,
            IConnectionProvider connectionProvider,
            IMqttTopicEvaluator topicEvaluator,
            IRepositoryProvider repositoryProvider,
            IPacketIdProvider packetIdProvider,
            ISubject<MqttUndeliveredMessage> undeliveredMessagesListener,
            MqttConfiguration configuration ) : base( authenticationProvider ?? NullAuthenticationProvider.Instance,
            connectionProvider,
            topicEvaluator,
            repositoryProvider,
            packetIdProvider,
            undeliveredMessagesListener,
            configuration
        )
        { }

        public class ServerFlowEventArg : EventArgs
        {
            public ProtocolFlowType Type { get; set; }
            public string ClientId { get; set; }
            public IPacket Input { get; set; }
            public IMqttChannel<IPacket> Channel { get; set; }
        }
        public EventHandler<ServerFlowEventArg> ProtocolFlowEvent;

        protected override IDictionary<ProtocolFlowType, IProtocolFlow> InitializeFlows()
        {
            return base.InitializeFlows().ToDictionary( k => k.Key, s => (IProtocolFlow) new TestFlowWrapper(
                s.Value,
                a => ProtocolFlowEvent?.Invoke( this, new ServerFlowEventArg
                {
                    Type = s.Key,
                    Channel = a.channel,
                    ClientId = a.clientId,
                    Input = a.input
                } ) ) );
        }
    }
}
