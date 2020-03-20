using CK.Core;
using CK.MQTT;

using CK.MQTT.Sdk;
using CK.MQTT.Sdk.Flows;
using CK.MQTT.Sdk.Packets;
using CK.MQTT.Sdk.Storage;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static CK.Testing.MonitorTestHelper;

namespace Tests.Flows
{
    public class SubscribeFlowSpec
    {
        [Test]
        public async Task when_subscribing_new_topics_then_subscriptions_are_created_and_ack_is_sent()
        {
            MqttConfiguration configuration = new MqttConfiguration { MaximumQualityOfService = MqttQualityOfService.AtLeastOnce };
            Mock<IMqttTopicEvaluator> topicEvaluator = new Mock<IMqttTopicEvaluator>();
            Mock<IRepository<ClientSession>> sessionRepository = new Mock<IRepository<ClientSession>>();
            IPacketIdProvider packetIdProvider = Mock.Of<IPacketIdProvider>();
            IRepository<RetainedMessage> retainedMessageRepository = Mock.Of<IRepository<RetainedMessage>>();
            IPublishSenderFlow senderFlow = Mock.Of<IPublishSenderFlow>();

            string clientId = Guid.NewGuid().ToString();
            ClientSession session = new ClientSession( clientId, clean: false );

            topicEvaluator.Setup( e => e.IsValidTopicFilter( It.IsAny<string>() ) ).Returns( true );
            sessionRepository.Setup( r => r.Read( It.IsAny<string>() ) ).Returns( session );

            MqttQualityOfService fooQoS = MqttQualityOfService.AtLeastOnce;
            string fooTopic = "test/foo/1";
            Subscription fooSubscription = new Subscription( fooTopic, fooQoS );
            MqttQualityOfService barQoS = MqttQualityOfService.AtMostOnce;
            string barTopic = "test/bar/1";
            Subscription barSubscription = new Subscription( barTopic, barQoS );

            ushort packetId = (ushort)new Random().Next( 0, ushort.MaxValue );
            Subscribe subscribe = new Subscribe( packetId, fooSubscription, barSubscription );

            Mock<IMqttChannel<IPacket>> channel = new Mock<IMqttChannel<IPacket>>();

            IPacket response = default;

            channel.Setup( c => c.SendAsync( It.IsAny<Mon<IPacket>>() ) )
                .Callback<Mon<IPacket>>( p => response = p.Item )
                .Returns( Task.Delay( 0 ) );

            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();

            connectionProvider
                .Setup( p => p.GetConnection( TestHelper.Monitor, It.Is<string>( c => c == clientId ) ) )
                .Returns( channel.Object );

            ServerSubscribeFlow flow = new ServerSubscribeFlow( topicEvaluator.Object, sessionRepository.Object,
                retainedMessageRepository, packetIdProvider, senderFlow, configuration );

            await flow.ExecuteAsync(TestHelper.Monitor, clientId, subscribe, channel.Object );

            sessionRepository.Verify( r => r.Update( It.Is<ClientSession>( s => s.Id == clientId && s.Subscriptions.Count == 2
                && s.Subscriptions.All( x => x.TopicFilter == fooTopic || x.TopicFilter == barTopic ) ) ) );
            Assert.NotNull( response );

            SubscribeAck subscribeAck = response as SubscribeAck;

            Assert.NotNull( subscribeAck );
            packetId.Should().Be( subscribeAck.PacketId );
            2.Should().Be( subscribeAck.ReturnCodes.Count() );
            Assert.True( subscribeAck.ReturnCodes.Any( c => c == SubscribeReturnCode.MaximumQoS0 ) );
            Assert.True( subscribeAck.ReturnCodes.Any( c => c == SubscribeReturnCode.MaximumQoS1 ) );
        }

        [Test]
        public async Task when_subscribing_existing_topics_then_subscriptions_are_updated_and_ack_is_sent()
        {
            MqttConfiguration configuration = new MqttConfiguration { MaximumQualityOfService = MqttQualityOfService.AtLeastOnce };
            Mock<IMqttTopicEvaluator> topicEvaluator = new Mock<IMqttTopicEvaluator>();
            Mock<IRepository<ClientSession>> sessionRepository = new Mock<IRepository<ClientSession>>();
            IPacketIdProvider packetIdProvider = Mock.Of<IPacketIdProvider>();
            IRepository<RetainedMessage> retainedMessageRepository = Mock.Of<IRepository<RetainedMessage>>();
            IPublishSenderFlow senderFlow = Mock.Of<IPublishSenderFlow>();

            string clientId = Guid.NewGuid().ToString();
            MqttQualityOfService fooQoS = MqttQualityOfService.AtLeastOnce;
            string fooTopic = "test/foo/1";
            Subscription fooSubscription = new Subscription( fooTopic, fooQoS );

            ClientSession session = new ClientSession( clientId, clean: false )
            {
                Subscriptions = new List<ClientSubscription> {
                    new ClientSubscription { ClientId = clientId, MaximumQualityOfService = MqttQualityOfService.ExactlyOnce, TopicFilter = fooTopic }
                }
            };

            topicEvaluator.Setup( e => e.IsValidTopicFilter( It.IsAny<string>() ) ).Returns( true );
            sessionRepository.Setup( r => r.Read( It.IsAny<string>() ) ).Returns( session );

            ushort packetId = (ushort)new Random().Next( 0, ushort.MaxValue );
            Subscribe subscribe = new Subscribe( packetId, fooSubscription );

            Mock<IMqttChannel<IPacket>> channel = new Mock<IMqttChannel<IPacket>>();

            IPacket response = default;

            channel.Setup( c => c.SendAsync( It.IsAny<Mon<IPacket>>() ) )
                .Callback<Mon<IPacket>>( p => response = p.Item )
                .Returns( Task.Delay( 0 ) );

            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();

            connectionProvider
                .Setup( p => p.GetConnection( TestHelper.Monitor, It.Is<string>( c => c == clientId ) ) )
                .Returns( channel.Object );

            ServerSubscribeFlow flow = new ServerSubscribeFlow( topicEvaluator.Object, sessionRepository.Object,
                retainedMessageRepository, packetIdProvider,
                senderFlow, configuration );

            await flow.ExecuteAsync(TestHelper.Monitor, clientId, subscribe, channel.Object );

            sessionRepository.Verify( r => r.Update( It.Is<ClientSession>( s => s.Id == clientId && s.Subscriptions.Count == 1
                && s.Subscriptions.Any( x => x.TopicFilter == fooTopic && x.MaximumQualityOfService == fooQoS ) ) ) );
            Assert.NotNull( response );

            SubscribeAck subscribeAck = response as SubscribeAck;

            Assert.NotNull( subscribeAck );
            packetId.Should().Be( subscribeAck.PacketId );
            1.Should().Be( subscribeAck.ReturnCodes.Count() );
            Assert.True( subscribeAck.ReturnCodes.Any( c => c == SubscribeReturnCode.MaximumQoS1 ) );
        }

        [Test]
        public async Task when_subscribing_invalid_topic_then_failure_is_sent_in_ack()
        {
            MqttConfiguration configuration = new MqttConfiguration { MaximumQualityOfService = MqttQualityOfService.AtLeastOnce };
            Mock<IMqttTopicEvaluator> topicEvaluator = new Mock<IMqttTopicEvaluator>();
            Mock<IRepository<ClientSession>> sessionRepository = new Mock<IRepository<ClientSession>>();
            IPacketIdProvider packetIdProvider = Mock.Of<IPacketIdProvider>();
            IRepository<RetainedMessage> retainedMessageRepository = Mock.Of<IRepository<RetainedMessage>>();
            IPublishSenderFlow senderFlow = Mock.Of<IPublishSenderFlow>();

            string clientId = Guid.NewGuid().ToString();
            ClientSession session = new ClientSession( clientId, clean: false );

            topicEvaluator.Setup( e => e.IsValidTopicFilter( It.IsAny<string>() ) ).Returns( false );
            sessionRepository.Setup( r => r.Read( It.IsAny<string>() ) ).Returns( session );

            MqttQualityOfService fooQoS = MqttQualityOfService.AtLeastOnce;
            string fooTopic = "test/foo/1";
            Subscription fooSubscription = new Subscription( fooTopic, fooQoS );

            ushort packetId = (ushort)new Random().Next( 0, ushort.MaxValue );
            Subscribe subscribe = new Subscribe( packetId, fooSubscription );

            Mock<IMqttChannel<IPacket>> channel = new Mock<IMqttChannel<IPacket>>();

            IPacket response = default;

            channel.Setup( c => c.SendAsync( It.IsAny<Mon<IPacket>>() ) )
                .Callback<Mon<IPacket>>( p => response = p.Item )
                .Returns( Task.Delay( 0 ) );

            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();

            connectionProvider
                .Setup( p => p.GetConnection( TestHelper.Monitor, It.Is<string>( c => c == clientId ) ) )
                .Returns( channel.Object );

            ServerSubscribeFlow flow = new ServerSubscribeFlow( topicEvaluator.Object, sessionRepository.Object,
                retainedMessageRepository, packetIdProvider,
                senderFlow, configuration );

            await flow.ExecuteAsync(TestHelper.Monitor, clientId, subscribe, channel.Object );

            Assert.NotNull( response );

            SubscribeAck subscribeAck = response as SubscribeAck;

            Assert.NotNull( subscribeAck );
            packetId.Should().Be( subscribeAck.PacketId );
            1.Should().Be( subscribeAck.ReturnCodes.Count() );
            SubscribeReturnCode.Failure.Should().Be( subscribeAck.ReturnCodes.First() );
        }

        [Test]
        public async Task when_subscribing_topic_with_retain_message_then_retained_is_sent()
        {
            MqttConfiguration configuration = new MqttConfiguration { MaximumQualityOfService = MqttQualityOfService.AtLeastOnce };
            Mock<IMqttTopicEvaluator> topicEvaluator = new Mock<IMqttTopicEvaluator>();
            Mock<IRepository<ClientSession>> sessionRepository = new Mock<IRepository<ClientSession>>();
            IPacketIdProvider packetIdProvider = Mock.Of<IPacketIdProvider>();
            Mock<IRepository<RetainedMessage>> retainedMessageRepository = new Mock<IRepository<RetainedMessage>>();
            Mock<IPublishSenderFlow> senderFlow = new Mock<IPublishSenderFlow>();

            string clientId = Guid.NewGuid().ToString();
            ClientSession session = new ClientSession( clientId, clean: false );

            sessionRepository.Setup( r => r.Read( It.IsAny<string>() ) ).Returns( session );

            MqttQualityOfService fooQoS = MqttQualityOfService.AtLeastOnce;
            string fooTopic = "test/foo/#";
            Subscription fooSubscription = new Subscription( fooTopic, fooQoS );

            string retainedTopic = "test/foo/bar";
            MqttQualityOfService retainedQoS = MqttQualityOfService.ExactlyOnce;
            byte[] retainedPayload = Encoding.UTF8.GetBytes( "Retained Message Test" );
            List<RetainedMessage> retainedMessages = new List<RetainedMessage> {
                new RetainedMessage (retainedTopic, retainedQoS, retainedPayload)
            };

            topicEvaluator.Setup( e => e.IsValidTopicFilter( It.IsAny<string>() ) ).Returns( true );
            topicEvaluator.Setup( e => e.Matches( It.IsAny<string>(), It.IsAny<string>() ) ).Returns( true );
            retainedMessageRepository.Setup( r => r.ReadAll() ).Returns( retainedMessages.AsQueryable() );

            ushort packetId = (ushort)new Random().Next( 0, ushort.MaxValue );
            Subscribe subscribe = new Subscribe( packetId, fooSubscription );

            Mock<IMqttChannel<IPacket>> channel = new Mock<IMqttChannel<IPacket>>();

            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();

            connectionProvider
                .Setup( p => p.GetConnection( TestHelper.Monitor, It.Is<string>( c => c == clientId ) ) )
                .Returns( channel.Object );

            ServerSubscribeFlow flow = new ServerSubscribeFlow( topicEvaluator.Object,
                sessionRepository.Object, retainedMessageRepository.Object,
                packetIdProvider, senderFlow.Object, configuration );

            await flow.ExecuteAsync(TestHelper.Monitor, clientId, subscribe, channel.Object );

            senderFlow.Verify( f => f.SendPublishAsync(TestHelper.Monitor, It.Is<string>( s => s == clientId ),
                It.Is<Publish>( p => p.Topic == retainedTopic &&
                    p.QualityOfService == fooQoS &&
                    p.Payload.ToArray().SequenceEqual( retainedPayload.ToArray() ) &&
                    p.PacketId.HasValue &&
                    p.Retain ),
                It.Is<IMqttChannel<IPacket>>( c => c == channel.Object ),
                It.Is<PendingMessageStatus>( x => x == PendingMessageStatus.PendingToSend ) ) );
        }
    }
}
