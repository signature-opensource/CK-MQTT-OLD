using CK.MQTT;
using CK.MQTT.Sdk;
using CK.MQTT.Sdk.Bindings;
using CK.MQTT.Sdk.Flows;
using CK.MQTT.Sdk.Packets;
using CK.MQTT.Sdk.Storage;
using IntegrationTests.Context;
using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace IntegrationTests
{
    public abstract class KeepAliveSpec : ConnectedContext
    {
        public KeepAliveSpec() : base( keepAliveSecs: 1 ) { }

        EventHandler<TestServerFlow.ServerFlowEventArg> _serverFlow;

        protected override IMqttServer Server
        {
            get
            {
                MqttTopicEvaluator topicEvaluator = new MqttTopicEvaluator( Configuration );
                IMqttChannelListener channelProvider = MqttServerBinding.GetChannelListener( Configuration );
                var channelFactory = new PacketChannelFactory( topicEvaluator, Configuration );
                var repositoryProvider = new InMemoryRepositoryProvider();
                var connectionProvider = new ConnectionProvider();
                var packetIdProvider = new PacketIdProvider();
                var undeliveredMessagesListener = new Subject<MqttUndeliveredMessage>();
                var flowProvider = new TestServerFlow( AuthenticationProvider, connectionProvider, topicEvaluator,
                    repositoryProvider, packetIdProvider, undeliveredMessagesListener, Configuration );

                flowProvider.ProtocolFlowEvent += ( sender, args ) => _serverFlow?.Invoke( sender, args );
                return new MqttServerImpl(
                    channelProvider,
                    channelFactory,
                    flowProvider,
                    connectionProvider,
                    undeliveredMessagesListener,
                    Configuration );
            }
        }

        [Test]
        public async Task keepalive_is_sent_automatically_by_client_and_received_by_server()
        {
            bool serverReceivedPing = false;
            void OnFlowEvent( object sender, TestServerFlow.ServerFlowEventArg args )
            {
                if( args.Type != ProtocolFlowType.Ping ) return;
                serverReceivedPing = true;
            }
            _serverFlow += OnFlowEvent;
            var client = await GetConnectedClientAsync();
            await Task.Delay( KeepAliveSecs * 1050 );//Keepalive to milliseconds + a small margin.
            serverReceivedPing.Should().BeTrue();
            _serverFlow -= OnFlowEvent;
        }

        [Test]
        public async Task keepalive_not_sent_if_client_send_messages()
        {
            bool serverReceivedPing = false;
            void OnFlowEvent( object sender, TestServerFlow.ServerFlowEventArg args )
            {
                if( args.Type != ProtocolFlowType.Ping ) return;
                serverReceivedPing = true;
            }
            _serverFlow += OnFlowEvent;
            var client = await GetConnectedClientAsync();
            await Task.Delay( KeepAliveSecs * 500 );
            await client.PublishAsync( new MqttApplicationMessage( "test", Array.Empty<byte>() ), MqttQualityOfService.ExactlyOnce );
            await Task.Delay( KeepAliveSecs * 700 );
            serverReceivedPing.Should().BeFalse();
            await Task.Delay( KeepAliveSecs * 550 );
            serverReceivedPing.Should().BeTrue();
            _serverFlow -= OnFlowEvent;
        }

    }
}
