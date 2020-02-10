using CK.MQTT;
using CK.MQTT.Sdk;
using CK.MQTT.Sdk.Flows;
using CK.MQTT.Sdk.Packets;
using CK.MQTT.Sdk.Storage;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using System;
using System.Reactive.Subjects;

namespace Tests
{
    public class ProtocolFlowProviderSpec
    {
        [TestCase( MqttPacketType.ConnectAck, typeof( ClientConnectFlow ) )]
        [TestCase( MqttPacketType.PingResponse, typeof( PingFlow ) )]
        [TestCase( MqttPacketType.Publish, typeof( PublishReceiverFlow ) )]
        [TestCase( MqttPacketType.PublishAck, typeof( PublishSenderFlow ) )]
        [TestCase( MqttPacketType.PublishReceived, typeof( PublishSenderFlow ) )]
        [TestCase( MqttPacketType.PublishRelease, typeof( PublishReceiverFlow ) )]
        [TestCase( MqttPacketType.PublishComplete, typeof( PublishSenderFlow ) )]
        [TestCase( MqttPacketType.SubscribeAck, typeof( ClientSubscribeFlow ) )]
        [TestCase( MqttPacketType.UnsubscribeAck, typeof( ClientUnsubscribeFlow ) )]
        public void when_getting_client_flow_from_valid_packet_type_then_succeeds( MqttPacketType packetType, Type flowType )
        {
            ClientProtocolFlowProvider flowProvider = new ClientProtocolFlowProvider( Mock.Of<IMqttTopicEvaluator>(), Mock.Of<IRepositoryProvider>(), new MqttConfiguration() );

            IProtocolFlow flow = flowProvider.GetFlow( packetType );
            flowType.Should().Be( flow.GetType() );
        }

        [TestCase( MqttPacketType.Connect, typeof( ServerConnectFlow ) )]
        [TestCase( MqttPacketType.Disconnect, typeof( DisconnectFlow ) )]
        [TestCase( MqttPacketType.PingRequest, typeof( PingFlow ) )]
        [TestCase( MqttPacketType.Publish, typeof( ServerPublishReceiverFlow ) )]
        [TestCase( MqttPacketType.PublishAck, typeof( PublishSenderFlow ) )]
        [TestCase( MqttPacketType.PublishReceived, typeof( PublishSenderFlow ) )]
        [TestCase( MqttPacketType.PublishRelease, typeof( ServerPublishReceiverFlow ) )]
        [TestCase( MqttPacketType.PublishComplete, typeof( PublishSenderFlow ) )]
        [TestCase( MqttPacketType.Subscribe, typeof( ServerSubscribeFlow ) )]
        [TestCase( MqttPacketType.Unsubscribe, typeof( ServerUnsubscribeFlow ) )]
        public void when_getting_server_flow_from_valid_packet_type_then_succeeds( MqttPacketType packetType, Type flowType )
        {
            IMqttAuthenticationProvider authenticationProvider = Mock.Of<IMqttAuthenticationProvider>( p => p.Authenticate( It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>() ) == true );
            ServerProtocolFlowProvider flowProvider = new ServerProtocolFlowProvider( authenticationProvider, Mock.Of<IConnectionProvider>(), Mock.Of<IMqttTopicEvaluator>(),
                Mock.Of<IRepositoryProvider>(), Mock.Of<IPacketIdProvider>(), Mock.Of<ISubject<MqttUndeliveredMessage>>(), new MqttConfiguration() );

            IProtocolFlow flow = flowProvider.GetFlow( packetType );

            flowType.Should().Be( flow.GetType() );
        }

        [Test]
        public void when_getting_explicit_server_flow_from_type_then_succeeds()
        {
            IMqttAuthenticationProvider authenticationProvider = Mock.Of<IMqttAuthenticationProvider>( p => p.Authenticate( It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>() ) == true );
            ServerProtocolFlowProvider flowProvider = new ServerProtocolFlowProvider( authenticationProvider, Mock.Of<IConnectionProvider>(), Mock.Of<IMqttTopicEvaluator>(),
                Mock.Of<IRepositoryProvider>(), Mock.Of<IPacketIdProvider>(), Mock.Of<ISubject<MqttUndeliveredMessage>>(), new MqttConfiguration() );

            ServerConnectFlow connectFlow = flowProvider.GetFlow<ServerConnectFlow>();
            PublishSenderFlow senderFlow = flowProvider.GetFlow<PublishSenderFlow>();
            ServerPublishReceiverFlow receiverFlow = flowProvider.GetFlow<ServerPublishReceiverFlow>();
            ServerSubscribeFlow subscribeFlow = flowProvider.GetFlow<ServerSubscribeFlow>();
            ServerUnsubscribeFlow unsubscribeFlow = flowProvider.GetFlow<ServerUnsubscribeFlow>();
            DisconnectFlow disconnectFlow = flowProvider.GetFlow<DisconnectFlow>();

            Assert.NotNull( connectFlow );
            Assert.NotNull( senderFlow );
            Assert.NotNull( receiverFlow );
            Assert.NotNull( subscribeFlow );
            Assert.NotNull( unsubscribeFlow );
            Assert.NotNull( disconnectFlow );
        }

        [Test]
        public void when_getting_explicit_client_flow_from_type_then_succeeds()
        {
            ClientProtocolFlowProvider flowProvider = new ClientProtocolFlowProvider( Mock.Of<IMqttTopicEvaluator>(), Mock.Of<IRepositoryProvider>(), new MqttConfiguration() );

            ClientConnectFlow connectFlow = flowProvider.GetFlow<ClientConnectFlow>();
            PublishSenderFlow senderFlow = flowProvider.GetFlow<PublishSenderFlow>();
            PublishReceiverFlow receiverFlow = flowProvider.GetFlow<PublishReceiverFlow>();
            ClientSubscribeFlow subscribeFlow = flowProvider.GetFlow<ClientSubscribeFlow>();
            ClientUnsubscribeFlow unsubscribeFlow = flowProvider.GetFlow<ClientUnsubscribeFlow>();
            PingFlow disconnectFlow = flowProvider.GetFlow<PingFlow>();

            Assert.NotNull( connectFlow );
            Assert.NotNull( senderFlow );
            Assert.NotNull( receiverFlow );
            Assert.NotNull( subscribeFlow );
            Assert.NotNull( unsubscribeFlow );
            Assert.NotNull( disconnectFlow );
        }
    }
}
