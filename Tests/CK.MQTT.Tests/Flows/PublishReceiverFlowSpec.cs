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
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using static CK.Testing.MonitorTestHelper;

namespace Tests.Flows
{
    [TestFixture]
    public class PublishReceiverFlowSpec
    {
        [Test]
        public async Task when_sending_publish_with_qos0_then_publish_is_sent_to_subscribers_and_no_ack_is_sent()
        {
            string clientId = Guid.NewGuid().ToString();

            MqttConfiguration configuration = new MqttConfiguration { MaximumQualityOfService = MqttQualityOfService.ExactlyOnce };
            Mock<IMqttTopicEvaluator> topicEvaluator = new Mock<IMqttTopicEvaluator>();
            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();
            Mock<IPublishSenderFlow> publishSenderFlow = new Mock<IPublishSenderFlow>();
            Mock<IRepository<RetainedMessage>> retainedRepository = new Mock<IRepository<RetainedMessage>>();
            Mock<IRepository<ClientSession>> sessionRepository = new Mock<IRepository<ClientSession>>();
            Mock<IRepository<ConnectionWill>> willRepository = new Mock<IRepository<ConnectionWill>>();

            sessionRepository.Setup( r => r.Read( It.IsAny<string>() ) )
                .Returns( new ClientSession( clientId ) { PendingMessages = new List<PendingMessage> { new PendingMessage() } } );

            IPacketIdProvider packetIdProvider = Mock.Of<IPacketIdProvider>();
            Subject<MqttUndeliveredMessage> undeliveredMessagesListener = new Subject<MqttUndeliveredMessage>();

            const string topic = "foo/bar";

            ServerPublishReceiverFlow flow = new ServerPublishReceiverFlow( topicEvaluator.Object, connectionProvider.Object,
                publishSenderFlow.Object, retainedRepository.Object, sessionRepository.Object, willRepository.Object,
                packetIdProvider, undeliveredMessagesListener, configuration );

            string subscribedClientId1 = Guid.NewGuid().ToString();
            string subscribedClientId2 = Guid.NewGuid().ToString();
            const MqttQualityOfService requestedQoS1 = MqttQualityOfService.AtLeastOnce;
            const MqttQualityOfService requestedQoS2 = MqttQualityOfService.ExactlyOnce;
            List<ClientSession> sessions = new List<ClientSession> {
                new ClientSession (subscribedClientId1, clean: false) {
                    Subscriptions = new List<ClientSubscription> {
                        new ClientSubscription { ClientId = subscribedClientId1,
                            MaximumQualityOfService = requestedQoS1, TopicFilter = topic }}
                },
                new ClientSession (subscribedClientId2, clean: false) {
                    Subscriptions = new List<ClientSubscription> {
                        new ClientSubscription { ClientId = subscribedClientId2,
                            MaximumQualityOfService = requestedQoS2, TopicFilter = topic }}
                }
            };

            Subject<Monitored<IPacket>> client1Receiver = new Subject<Monitored<IPacket>>();
            Mock<IMqttChannel<IPacket>> client1Channel = new Mock<IMqttChannel<IPacket>>();

            client1Channel.Setup( c => c.ReceiverStream ).Returns( client1Receiver );

            Subject<Monitored<IPacket>> client2Receiver = new Subject<Monitored<IPacket>>();
            Mock<IMqttChannel<IPacket>> client2Channel = new Mock<IMqttChannel<IPacket>>();

            client2Channel.Setup( c => c.ReceiverStream ).Returns( client2Receiver );

            topicEvaluator.Setup( e => e.Matches( It.IsAny<string>(), It.IsAny<string>() ) ).Returns( true );
            sessionRepository.Setup( r => r.ReadAll() ).Returns( sessions.AsQueryable() );

            connectionProvider
                .Setup( p => p.GetConnection( TestHelper.Monitor, It.Is<string>( s => s == subscribedClientId1 ) ) )
                .Returns( client1Channel.Object );
            connectionProvider
                .Setup( p => p.GetConnection( TestHelper.Monitor, It.Is<string>( s => s == subscribedClientId2 ) ) )
                .Returns( client2Channel.Object );

            Publish publish = new Publish( topic, MqttQualityOfService.AtMostOnce, retain: false, duplicated: false )
            {
                Payload = Encoding.UTF8.GetBytes( "Publish Receiver Flow Test" )
            };

            Subject<Monitored<IPacket>> receiver = new Subject<Monitored<IPacket>>();
            Mock<IMqttChannel<IPacket>> channel = new Mock<IMqttChannel<IPacket>>();

            channel.Setup( c => c.ReceiverStream ).Returns( receiver );

            await flow.ExecuteAsync( TestHelper.Monitor, clientId, publish, channel.Object );

            retainedRepository.Verify( r => r.Create( It.IsAny<RetainedMessage>() ), Times.Never );
            publishSenderFlow.Verify( s => s.SendPublishAsync( TestHelper.Monitor, It.Is<string>( x => x == subscribedClientId1 ),
                It.Is<Publish>( p => p.Topic == publish.Topic &&
                    p.Payload.ToList().SequenceEqual( publish.Payload ) ),
                It.Is<IMqttChannel<IPacket>>( c => c == client1Channel.Object ), It.Is<PendingMessageStatus>( x => x == PendingMessageStatus.PendingToSend ) ) );
            publishSenderFlow.Verify( s => s.SendPublishAsync( TestHelper.Monitor, It.Is<string>( x => x == subscribedClientId2 ),
                It.Is<Publish>( p => p.Topic == publish.Topic &&
                    p.Payload.ToList().SequenceEqual( publish.Payload ) ),
                It.Is<IMqttChannel<IPacket>>( c => c == client2Channel.Object ), It.Is<PendingMessageStatus>( x => x == PendingMessageStatus.PendingToSend ) ) );
            channel.Verify( c => c.SendAsync( It.IsAny<Monitored<IPacket>>() ), Times.Never );
        }

        [Test]
        public async Task when_sending_publish_with_qos1_then_publish_is_sent_to_subscribers_and_publish_ack_is_sent()
        {
            string clientId = Guid.NewGuid().ToString();

            MqttConfiguration configuration = new MqttConfiguration { MaximumQualityOfService = MqttQualityOfService.ExactlyOnce };
            Mock<IMqttTopicEvaluator> topicEvaluator = new Mock<IMqttTopicEvaluator>();
            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();
            Mock<IPublishSenderFlow> publishSenderFlow = new Mock<IPublishSenderFlow>();
            Mock<IRepository<RetainedMessage>> retainedRepository = new Mock<IRepository<RetainedMessage>>();
            Mock<IRepository<ClientSession>> sessionRepository = new Mock<IRepository<ClientSession>>();
            Mock<IRepository<ConnectionWill>> willRepository = new Mock<IRepository<ConnectionWill>>();

            sessionRepository.Setup( r => r.Read( It.IsAny<string>() ) )
                .Returns( new ClientSession( clientId )
                {
                    PendingMessages = new List<PendingMessage> { new PendingMessage() }
                } );

            IPacketIdProvider packetIdProvider = Mock.Of<IPacketIdProvider>();
            Subject<MqttUndeliveredMessage> undeliveredMessagesListener = new Subject<MqttUndeliveredMessage>();

            string topic = "foo/bar";

            ServerPublishReceiverFlow flow = new ServerPublishReceiverFlow( topicEvaluator.Object, connectionProvider.Object, publishSenderFlow.Object,
                retainedRepository.Object, sessionRepository.Object, willRepository.Object,
                packetIdProvider, undeliveredMessagesListener, configuration );

            string subscribedClientId = Guid.NewGuid().ToString();
            MqttQualityOfService requestedQoS = MqttQualityOfService.ExactlyOnce;
            List<ClientSession> sessions = new List<ClientSession> { new ClientSession (subscribedClientId, clean: false) {
                Subscriptions = new List<ClientSubscription> {
                    new ClientSubscription { ClientId = subscribedClientId, MaximumQualityOfService = requestedQoS, TopicFilter = topic }
                }
            }};

            Subject<Monitored<IPacket>> clientReceiver = new Subject<Monitored<IPacket>>();
            Mock<IMqttChannel<IPacket>> clientChannel = new Mock<IMqttChannel<IPacket>>();

            clientChannel.Setup( c => c.ReceiverStream ).Returns( clientReceiver );

            topicEvaluator.Setup( e => e.Matches( It.IsAny<string>(), It.IsAny<string>() ) ).Returns( true );
            sessionRepository.Setup( r => r.ReadAll() ).Returns( sessions.AsQueryable() );

            connectionProvider
                .Setup( p => p.GetConnection( TestHelper.Monitor, It.Is<string>( s => s == subscribedClientId ) ) )
                .Returns( clientChannel.Object );

            ushort? packetId = (ushort?)new Random().Next( 0, ushort.MaxValue );
            Publish publish = new Publish( topic, MqttQualityOfService.AtLeastOnce, retain: false, duplicated: false, packetId: packetId )
            {
                Payload = Encoding.UTF8.GetBytes( "Publish Flow Test" )
            };

            Subject<Monitored<IPacket>> receiver = new Subject<Monitored<IPacket>>();
            Mock<IMqttChannel<IPacket>> channel = new Mock<IMqttChannel<IPacket>>();

            channel.Setup( c => c.IsConnected ).Returns( true );
            channel.Setup( c => c.ReceiverStream ).Returns( receiver );

            await flow.ExecuteAsync( TestHelper.Monitor, clientId, publish, channel.Object );

            retainedRepository.Verify( r => r.Create( It.IsAny<RetainedMessage>() ), Times.Never );
            publishSenderFlow.Verify( s => s.SendPublishAsync( TestHelper.Monitor, It.Is<string>( x => x == subscribedClientId ),
                It.Is<Publish>( p => p.Topic == publish.Topic &&
                    p.Payload.ToList().SequenceEqual( publish.Payload ) ),
                It.Is<IMqttChannel<IPacket>>( c => c == clientChannel.Object ), It.Is<PendingMessageStatus>( x => x == PendingMessageStatus.PendingToSend ) ) );
            channel.Verify( c => c.SendAsync( It.Is<Monitored<IPacket>>( p => p.Item is PublishAck &&
                ((PublishAck)p.Item).PacketId == packetId.Value ) ) );
        }

        [Test]
        public async Task when_sending_publish_with_qos2_then_publish_is_sent_to_subscribers_and_publish_received_is_sent()
        {
            Assume.That( false, "To be investigated and fixed accordingly." );

            string clientId = Guid.NewGuid().ToString();

            MqttConfiguration configuration = new MqttConfiguration { MaximumQualityOfService = MqttQualityOfService.ExactlyOnce };
            Mock<IMqttTopicEvaluator> topicEvaluator = new Mock<IMqttTopicEvaluator>();
            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();
            Mock<IPublishSenderFlow> publishSenderFlow = new Mock<IPublishSenderFlow>();
            Mock<IRepository<RetainedMessage>> retainedRepository = new Mock<IRepository<RetainedMessage>>();
            Mock<IRepository<ClientSession>> sessionRepository = new Mock<IRepository<ClientSession>>();
            Mock<IRepository<ConnectionWill>> willRepository = new Mock<IRepository<ConnectionWill>>();

            publishSenderFlow.Setup( f => f.SendPublishAsync( TestHelper.Monitor, It.IsAny<string>(), It.IsAny<Publish>(), It.IsAny<IMqttChannel<IPacket>>(), It.IsAny<PendingMessageStatus>() ) )
                .Returns( Task.Delay( 0 ) );

            sessionRepository.Setup( r => r.Read( It.IsAny<string>() ) )
                .Returns( new ClientSession( clientId )
                {
                    PendingMessages = new List<PendingMessage> { new PendingMessage() }
                } );

            ushort? packetId = (ushort?)new Random().Next( 0, ushort.MaxValue );
            IPacketIdProvider packetIdProvider = Mock.Of<IPacketIdProvider>();
            Subject<MqttUndeliveredMessage> undeliveredMessagesListener = new Subject<MqttUndeliveredMessage>();

            const string topic = "foo/bar";

            ServerPublishReceiverFlow flow = new ServerPublishReceiverFlow( topicEvaluator.Object, connectionProvider.Object, publishSenderFlow.Object,
                retainedRepository.Object, sessionRepository.Object, willRepository.Object, packetIdProvider, undeliveredMessagesListener, configuration );

            string subscribedClientId = Guid.NewGuid().ToString();
            MqttQualityOfService requestedQoS = MqttQualityOfService.ExactlyOnce;
            List<ClientSession> sessions = new List<ClientSession> { new ClientSession (subscribedClientId, clean: false) {
                Subscriptions = new List<ClientSubscription> {
                    new ClientSubscription { ClientId = subscribedClientId, MaximumQualityOfService = requestedQoS, TopicFilter = topic }
                }
            }};

            Subject<Monitored<IPacket>> clientReceiver = new Subject<Monitored<IPacket>>();
            Mock<IMqttChannel<IPacket>> clientChannel = new Mock<IMqttChannel<IPacket>>();

            clientChannel.Setup( c => c.ReceiverStream ).Returns( clientReceiver );

            topicEvaluator.Setup( e => e.Matches( It.IsAny<string>(), It.IsAny<string>() ) ).Returns( true );
            sessionRepository.Setup( r => r.ReadAll() ).Returns( sessions.AsQueryable() );

            connectionProvider
                .Setup( p => p.GetConnection( TestHelper.Monitor, It.Is<string>( s => s == subscribedClientId ) ) )
                .Returns( clientChannel.Object );

            Publish publish = new Publish( topic, MqttQualityOfService.ExactlyOnce, retain: false, duplicated: false, packetId: packetId )
            {
                Payload = Encoding.UTF8.GetBytes( "Publish Flow Test" )
            };

            Subject<Monitored<IPacket>> receiver = new Subject<Monitored<IPacket>>();
            Subject<Monitored<IPacket>> sender = new Subject<Monitored<IPacket>>();
            Mock<IMqttChannel<IPacket>> channelMock = new Mock<IMqttChannel<IPacket>>();

            channelMock.Setup( c => c.IsConnected ).Returns( true );
            channelMock.Setup( c => c.ReceiverStream ).Returns( receiver );
            channelMock.Setup( c => c.SenderStream ).Returns( sender );
            channelMock.Setup( c => c.SendAsync( It.IsAny<Monitored<IPacket>>() ) )
                .Callback<Monitored<IPacket>>( packet => sender.OnNext( packet ) )
                .Returns( Task.Delay( 0 ) );

            IMqttChannel<IPacket> channel = channelMock.Object;
            ManualResetEventSlim ackSentSignal = new ManualResetEventSlim( initialState: false );

            sender.Subscribe( p =>
            {
                if( p.Item is PublishReceived )
                {
                    ackSentSignal.Set();
                }
            } );

            Task flowTask = flow.ExecuteAsync( TestHelper.Monitor, clientId, publish, channel );

            bool ackSent = ackSentSignal.Wait( 1000 );

            receiver.OnNext( new Monitored<IPacket>( TestHelper.Monitor, new PublishRelease( packetId.Value ) ) );

            await Task.Delay( TimeSpan.FromMilliseconds( 1000 ) );

            Assert.True( ackSent );
            publishSenderFlow.Verify( s => s.SendPublishAsync( TestHelper.Monitor, It.Is<string>( x => x == subscribedClientId ),
                It.Is<Publish>( p => p.Topic == publish.Topic &&
                    p.Payload.ToList().SequenceEqual( publish.Payload ) ),
                It.Is<IMqttChannel<IPacket>>( c => c == clientChannel.Object ), It.Is<PendingMessageStatus>( x => x == PendingMessageStatus.PendingToSend ) ) );
            retainedRepository.Verify( r => r.Create( It.IsAny<RetainedMessage>() ), Times.Never );
            channelMock.Verify( c => c.SendAsync( It.Is<Monitored<IPacket>>( p => p.Item is PublishReceived && (p.Item as PublishReceived).PacketId == packetId.Value ) ) );
        }

        [Test]
        public void when_sending_publish_with_qos2_and_no_release_is_sent_after_receiving_publish_received_then_publish_received_is_re_transmitted()
        {
            string clientId = Guid.NewGuid().ToString();

            MqttConfiguration configuration = new MqttConfiguration
            {
                MaximumQualityOfService = MqttQualityOfService.ExactlyOnce,
                WaitTimeoutSecs = 1
            };
            Mock<IMqttTopicEvaluator> topicEvaluator = new Mock<IMqttTopicEvaluator>();
            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();
            Mock<IPublishSenderFlow> publishSenderFlow = new Mock<IPublishSenderFlow>();
            Mock<IRepository<RetainedMessage>> retainedRepository = new Mock<IRepository<RetainedMessage>>();
            Mock<IRepository<ClientSession>> sessionRepository = new Mock<IRepository<ClientSession>>();
            Mock<IRepository<ConnectionWill>> willRepository = new Mock<IRepository<ConnectionWill>>();

            sessionRepository.Setup( r => r.Read( It.IsAny<string>() ) )
                .Returns( new ClientSession( clientId )
                {
                    PendingMessages = new List<PendingMessage> { new PendingMessage() }
                } );

            IPacketIdProvider packetIdProvider = Mock.Of<IPacketIdProvider>();
            Subject<MqttUndeliveredMessage> undeliveredMessagesListener = new Subject<MqttUndeliveredMessage>();

            string topic = "foo/bar";

            ServerPublishReceiverFlow flow = new ServerPublishReceiverFlow( topicEvaluator.Object, connectionProvider.Object, publishSenderFlow.Object,
                retainedRepository.Object, sessionRepository.Object, willRepository.Object, packetIdProvider, undeliveredMessagesListener, configuration );

            string subscribedClientId = Guid.NewGuid().ToString();
            MqttQualityOfService requestedQoS = MqttQualityOfService.ExactlyOnce;
            List<ClientSession> sessions = new List<ClientSession> { new ClientSession (subscribedClientId, clean: false) {
                Subscriptions = new List<ClientSubscription> {
                    new ClientSubscription { ClientId = subscribedClientId, MaximumQualityOfService = requestedQoS, TopicFilter = topic }
                }
            }};

            Subject<Monitored<IPacket>> clientReceiver = new Subject<Monitored<IPacket>>();
            Mock<IMqttChannel<IPacket>> clientChannel = new Mock<IMqttChannel<IPacket>>();

            clientChannel.Setup( c => c.ReceiverStream ).Returns( clientReceiver );

            topicEvaluator.Setup( e => e.Matches( It.IsAny<string>(), It.IsAny<string>() ) ).Returns( true );
            sessionRepository.Setup( r => r.ReadAll() ).Returns( sessions.AsQueryable() );

            ushort? packetId = (ushort?)new Random().Next( 0, ushort.MaxValue );
            Publish publish = new Publish( topic, MqttQualityOfService.ExactlyOnce, retain: false, duplicated: false, packetId: packetId )
            {
                Payload = Encoding.UTF8.GetBytes( "Publish Receiver Flow Test" )
            };

            Subject<Monitored<IPacket>> receiver = new Subject<Monitored<IPacket>>();
            Subject<Monitored<IPacket>> sender = new Subject<Monitored<IPacket>>();
            Mock<IMqttChannel<IPacket>> channel = new Mock<IMqttChannel<IPacket>>();

            channel.Setup( c => c.IsConnected ).Returns( true );
            channel.Setup( c => c.ReceiverStream ).Returns( receiver );
            channel.Setup( c => c.SenderStream ).Returns( sender );
            channel.Setup( c => c.SendAsync( It.IsAny<Monitored<IPacket>>() ) )
                .Callback<Monitored<IPacket>>( packet => sender.OnNext( packet ) )
                .Returns( Task.Delay( 0 ) );

            ManualResetEventSlim publishReceivedSignal = new ManualResetEventSlim( initialState: false );
            int retries = 0;

            sender.Subscribe( packet =>
            {
                if( packet.Item is PublishReceived )
                {
                    retries++;
                }

                if( retries > 1 )
                {
                    publishReceivedSignal.Set();
                }
            } );

            Task flowTask = flow.ExecuteAsync( TestHelper.Monitor, clientId, publish, channel.Object );

            bool retried = publishReceivedSignal.Wait( 2000 );

            Assert.True( retried );
            channel.Verify( c => c.SendAsync( It.Is<Monitored<IPacket>>( p => p.Item is PublishReceived
                && (p.Item as PublishReceived).PacketId == packetId ) ), Times.AtLeast( 2 ) );
        }

        [Test]
        public async Task when_sending_publish_with_retain_then_retain_message_is_created()
        {
            string clientId = Guid.NewGuid().ToString();

            MqttConfiguration configuration = Mock.Of<MqttConfiguration>();
            Mock<IMqttTopicEvaluator> topicEvaluator = new Mock<IMqttTopicEvaluator>();
            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();
            Mock<IPublishSenderFlow> publishSenderFlow = new Mock<IPublishSenderFlow>();
            Mock<IRepository<RetainedMessage>> retainedRepository = new Mock<IRepository<RetainedMessage>>();
            Mock<IRepository<ClientSession>> sessionRepository = new Mock<IRepository<ClientSession>>();
            Mock<IRepository<ConnectionWill>> willRepository = new Mock<IRepository<ConnectionWill>>();

            sessionRepository.Setup( r => r.Read( It.IsAny<string>() ) )
                .Returns( new ClientSession( clientId )
                {
                    PendingMessages = new List<PendingMessage> { new PendingMessage() }
                } );

            IPacketIdProvider packetIdProvider = Mock.Of<IPacketIdProvider>();
            Subject<MqttUndeliveredMessage> undeliveredMessagesListener = new Subject<MqttUndeliveredMessage>();

            string topic = "foo/bar";

            List<ClientSession> sessions = new List<ClientSession> { new ClientSession( Guid.NewGuid().ToString(), clean: false ) };

            retainedRepository.Setup( r => r.Read( It.IsAny<string>() ) ).Returns( default( RetainedMessage ) );
            sessionRepository.Setup( r => r.ReadAll() ).Returns( sessions.AsQueryable() );

            MqttQualityOfService qos = MqttQualityOfService.AtMostOnce;
            string payload = "Publish Flow Test";
            Publish publish = new Publish( topic, qos, retain: true, duplicated: false )
            {
                Payload = Encoding.UTF8.GetBytes( payload )
            };

            Subject<Monitored<IPacket>> receiver = new Subject<Monitored<IPacket>>();
            Mock<IMqttChannel<IPacket>> channel = new Mock<IMqttChannel<IPacket>>();

            channel.Setup( c => c.ReceiverStream ).Returns( receiver );

            ServerPublishReceiverFlow flow = new ServerPublishReceiverFlow( topicEvaluator.Object, connectionProvider.Object, publishSenderFlow.Object,
                retainedRepository.Object, sessionRepository.Object, willRepository.Object, packetIdProvider, undeliveredMessagesListener, configuration );

            await flow.ExecuteAsync( TestHelper.Monitor, clientId, publish, channel.Object );

            retainedRepository.Verify( r => r.Create( It.Is<RetainedMessage>( m => m.Id == topic && m.QualityOfService == qos && m.Payload.ToList().SequenceEqual( publish.Payload ) ) ) );
            channel.Verify( c => c.SendAsync( It.IsAny<Monitored<IPacket>>() ), Times.Never );
        }

        [Test]
        public async Task when_sending_publish_with_retain_then_retain_message_is_replaced()
        {
            string clientId = Guid.NewGuid().ToString();

            MqttConfiguration configuration = Mock.Of<MqttConfiguration>();
            Mock<IMqttTopicEvaluator> topicEvaluator = new Mock<IMqttTopicEvaluator>();
            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();
            Mock<IPublishSenderFlow> publishSenderFlow = new Mock<IPublishSenderFlow>();
            Mock<IRepository<RetainedMessage>> retainedRepository = new Mock<IRepository<RetainedMessage>>();
            Mock<IRepository<ClientSession>> sessionRepository = new Mock<IRepository<ClientSession>>();
            Mock<IRepository<ConnectionWill>> willRepository = new Mock<IRepository<ConnectionWill>>();

            sessionRepository.Setup( r => r.Read( It.IsAny<string>() ) )
                .Returns( new ClientSession( clientId )
                {
                    PendingMessages = new List<PendingMessage> { new PendingMessage() }
                } );

            IPacketIdProvider packetIdProvider = Mock.Of<IPacketIdProvider>();
            Subject<MqttUndeliveredMessage> undeliveredMessagesListener = new Subject<MqttUndeliveredMessage>();

            string topic = "foo/bar";

            List<ClientSession> sessions = new List<ClientSession> { new ClientSession( Guid.NewGuid().ToString(), clean: false ) };

            RetainedMessage existingRetainedMessage = new RetainedMessage( topic: "foo", qos: MqttQualityOfService.AtLeastOnce, payload: new byte[100] );

            retainedRepository.Setup( r => r.Read( It.IsAny<string>() ) ).Returns( existingRetainedMessage );
            sessionRepository.Setup( r => r.ReadAll() ).Returns( sessions.AsQueryable() );

            MqttQualityOfService qos = MqttQualityOfService.AtMostOnce;
            string payload = "Publish Flow Test";
            Publish publish = new Publish( topic, qos, retain: true, duplicated: false )
            {
                Payload = Encoding.UTF8.GetBytes( payload )
            };

            Subject<Monitored<IPacket>> receiver = new Subject<Monitored<IPacket>>();
            Mock<IMqttChannel<IPacket>> channel = new Mock<IMqttChannel<IPacket>>();

            channel.Setup( c => c.ReceiverStream ).Returns( receiver );

            ServerPublishReceiverFlow flow = new ServerPublishReceiverFlow( topicEvaluator.Object, connectionProvider.Object, publishSenderFlow.Object,
                retainedRepository.Object, sessionRepository.Object, willRepository.Object, packetIdProvider, undeliveredMessagesListener, configuration );

            await flow.ExecuteAsync( TestHelper.Monitor, clientId, publish, channel.Object );

            retainedRepository.Verify( r => r.Delete( It.Is<string>( m => m == existingRetainedMessage.Id ) ) );
            retainedRepository.Verify( r => r.Create( It.Is<RetainedMessage>( m => m.Id == topic && m.QualityOfService == qos && m.Payload.ToList().SequenceEqual( publish.Payload ) ) ) );
            channel.Verify( c => c.SendAsync( It.IsAny<Monitored<IPacket>>() ), Times.Never );
        }

        [Test]
        public async Task when_sending_publish_with_qos_higher_than_supported_then_supported_is_used()
        {
            string clientId = Guid.NewGuid().ToString();

            MqttConfiguration configuration = new MqttConfiguration { MaximumQualityOfService = MqttQualityOfService.AtLeastOnce };
            Mock<IMqttTopicEvaluator> topicEvaluator = new Mock<IMqttTopicEvaluator>();
            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();
            Mock<IPublishSenderFlow> publishSenderFlow = new Mock<IPublishSenderFlow>();
            Mock<IRepository<RetainedMessage>> retainedRepository = new Mock<IRepository<RetainedMessage>>();
            Mock<IRepository<ClientSession>> sessionRepository = new Mock<IRepository<ClientSession>>();
            Mock<IRepository<ConnectionWill>> willRepository = new Mock<IRepository<ConnectionWill>>();

            sessionRepository.Setup( r => r.Read( It.IsAny<string>() ) )
                .Returns( new ClientSession( clientId )
                {
                    PendingMessages = new List<PendingMessage> { new PendingMessage() }
                } );

            IPacketIdProvider packetIdProvider = Mock.Of<IPacketIdProvider>();
            Subject<MqttUndeliveredMessage> undeliveredMessagesListener = new Subject<MqttUndeliveredMessage>();

            string topic = "foo/bar";

            ServerPublishReceiverFlow flow = new ServerPublishReceiverFlow( topicEvaluator.Object, connectionProvider.Object, publishSenderFlow.Object,
                retainedRepository.Object, sessionRepository.Object, willRepository.Object, packetIdProvider, undeliveredMessagesListener, configuration );

            string subscribedClientId = Guid.NewGuid().ToString();
            MqttQualityOfService requestedQoS = MqttQualityOfService.ExactlyOnce;
            List<ClientSession> sessions = new List<ClientSession> { new ClientSession(subscribedClientId, clean: false) {
                Subscriptions = new List<ClientSubscription> {
                    new ClientSubscription { ClientId = subscribedClientId, MaximumQualityOfService = requestedQoS, TopicFilter = topic }
                }
            }};

            Subject<Monitored<IPacket>> clientReceiver = new Subject<Monitored<IPacket>>();
            Mock<IMqttChannel<IPacket>> clientChannel = new Mock<IMqttChannel<IPacket>>();

            clientChannel.Setup( c => c.ReceiverStream ).Returns( clientReceiver );

            topicEvaluator.Setup( e => e.Matches( It.IsAny<string>(), It.IsAny<string>() ) ).Returns( true );
            sessionRepository.Setup( r => r.ReadAll() ).Returns( sessions.AsQueryable() );

            connectionProvider
                .Setup( p => p.GetConnection( TestHelper.Monitor, It.Is<string>( s => s == subscribedClientId ) ) )
                .Returns( clientChannel.Object );

            ushort? packetId = (ushort?)new Random().Next( 0, ushort.MaxValue );
            Publish publish = new Publish( topic, MqttQualityOfService.ExactlyOnce, retain: false, duplicated: false, packetId: packetId )
            {
                Payload = Encoding.UTF8.GetBytes( "Publish Flow Test" )
            };

            Subject<Monitored<IPacket>> receiver = new Subject<Monitored<IPacket>>();
            Mock<IMqttChannel<IPacket>> channel = new Mock<IMqttChannel<IPacket>>();

            channel.Setup( c => c.IsConnected ).Returns( true );
            channel.Setup( c => c.ReceiverStream ).Returns( receiver );

            await flow.ExecuteAsync( TestHelper.Monitor, clientId, publish, channel.Object );

            publishSenderFlow.Verify( s => s.SendPublishAsync( TestHelper.Monitor, It.Is<string>( x => x == subscribedClientId ),
                It.Is<Publish>( p => p.Topic == publish.Topic &&
                    p.Payload.ToList().SequenceEqual( publish.Payload ) ),
                It.Is<IMqttChannel<IPacket>>( c => c == clientChannel.Object ), It.Is<PendingMessageStatus>( x => x == PendingMessageStatus.PendingToSend ) ) );
            retainedRepository.Verify( r => r.Create( It.IsAny<RetainedMessage>() ), Times.Never );
            channel.Verify( c => c.SendAsync( It.Is<Monitored<IPacket>>( p => p.Item is PublishAck && (p.Item as PublishAck).PacketId == packetId.Value ) ) );
        }

        [Test]
        public void when_sending_publish_with_qos_higher_than_zero_and_without_packet_id_then_fails()
        {
            string clientId = Guid.NewGuid().ToString();

            MqttConfiguration configuration = new MqttConfiguration { MaximumQualityOfService = MqttQualityOfService.ExactlyOnce };
            Mock<IMqttTopicEvaluator> topicEvaluator = new Mock<IMqttTopicEvaluator>();
            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();
            Mock<IPublishSenderFlow> publishSenderFlow = new Mock<IPublishSenderFlow>();
            IRepository<RetainedMessage> retainedRepository = Mock.Of<IRepository<RetainedMessage>>();
            Mock<IRepository<ClientSession>> sessionRepository = new Mock<IRepository<ClientSession>>();
            Mock<IRepository<ConnectionWill>> willRepository = new Mock<IRepository<ConnectionWill>>();

            sessionRepository.Setup( r => r.Read( It.IsAny<string>() ) )
                .Returns( new ClientSession( clientId )
                {
                    PendingMessages = new List<PendingMessage> { new PendingMessage() }
                } );

            IPacketIdProvider packetIdProvider = Mock.Of<IPacketIdProvider>();
            Subject<MqttUndeliveredMessage> undeliveredMessagesListener = new Subject<MqttUndeliveredMessage>();

            string topic = "foo/bar";

            string subscribedClientId = Guid.NewGuid().ToString();
            List<ClientSession> sessions = new List<ClientSession> { new ClientSession( subscribedClientId, clean: false ) };

            sessionRepository.Setup( r => r.ReadAll() ).Returns( sessions.AsQueryable() );

            Publish publish = new Publish( topic, MqttQualityOfService.AtLeastOnce, retain: false, duplicated: false )
            {
                Payload = Encoding.UTF8.GetBytes( "Publish Flow Test" )
            };

            Subject<Monitored<IPacket>> receiver = new Subject<Monitored<IPacket>>();
            Mock<IMqttChannel<IPacket>> channel = new Mock<IMqttChannel<IPacket>>();

            channel.Setup( c => c.ReceiverStream ).Returns( receiver );

            ServerPublishReceiverFlow flow = new ServerPublishReceiverFlow( topicEvaluator.Object, connectionProvider.Object, publishSenderFlow.Object,
                retainedRepository, sessionRepository.Object, willRepository.Object, packetIdProvider, undeliveredMessagesListener, configuration );

            AggregateException ex = Assert.Throws<AggregateException>( () => flow.ExecuteAsync( TestHelper.Monitor, clientId, publish, channel.Object ).Wait() );

            Assert.True( ex.InnerException is MqttException );
        }

        [Test]
        public async Task when_sending_publish_and_subscriber_with_qos1_send_publish_ack_then_publish_is_not_re_transmitted()
        {
            string clientId = Guid.NewGuid().ToString();

            MqttConfiguration configuration = new MqttConfiguration
            {
                MaximumQualityOfService = MqttQualityOfService.ExactlyOnce,
                WaitTimeoutSecs = 2
            };
            Mock<IMqttTopicEvaluator> topicEvaluator = new Mock<IMqttTopicEvaluator>();
            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();
            Mock<IPublishSenderFlow> publishSenderFlow = new Mock<IPublishSenderFlow>();
            Mock<IRepository<RetainedMessage>> retainedRepository = new Mock<IRepository<RetainedMessage>>();
            Mock<IRepository<ClientSession>> sessionRepository = new Mock<IRepository<ClientSession>>();
            Mock<IRepository<ConnectionWill>> willRepository = new Mock<IRepository<ConnectionWill>>();

            sessionRepository.Setup( r => r.Read( It.IsAny<string>() ) )
                .Returns( new ClientSession( clientId )
                {
                    PendingMessages = new List<PendingMessage> { new PendingMessage() }
                } );

            IPacketIdProvider packetIdProvider = Mock.Of<IPacketIdProvider>();
            Subject<MqttUndeliveredMessage> undeliveredMessagesListener = new Subject<MqttUndeliveredMessage>();

            string topic = "foo/bar";

            ServerPublishReceiverFlow flow = new ServerPublishReceiverFlow( topicEvaluator.Object, connectionProvider.Object, publishSenderFlow.Object,
                retainedRepository.Object, sessionRepository.Object, willRepository.Object, packetIdProvider, undeliveredMessagesListener, configuration );

            string subscribedClientId = Guid.NewGuid().ToString();
            MqttQualityOfService requestedQoS = MqttQualityOfService.AtLeastOnce;
            List<ClientSession> sessions = new List<ClientSession> { new ClientSession (subscribedClientId, clean: false) {
                Subscriptions = new List<ClientSubscription> {
                    new ClientSubscription { ClientId = subscribedClientId, MaximumQualityOfService = requestedQoS, TopicFilter = topic }
                }
            }};

            Subject<Monitored<IPacket>> clientReceiver = new Subject<Monitored<IPacket>>();
            Subject<Monitored<IPacket>> clientSender = new Subject<Monitored<IPacket>>();
            Mock<IMqttChannel<IPacket>> clientChannel = new Mock<IMqttChannel<IPacket>>();

            clientSender.OfType<Publish>().Subscribe( p =>
            {
                clientReceiver.OnNext( new Monitored<IPacket>( TestHelper.Monitor, new PublishAck( p.PacketId.Value ) ) );
            } );

            clientChannel.Setup( c => c.ReceiverStream ).Returns( clientReceiver );
            clientChannel.Setup( c => c.SenderStream ).Returns( clientSender );
            clientChannel.Setup( c => c.SendAsync( It.IsAny<Monitored<IPacket>>() ) )
                .Callback<IPacket>( packet => clientSender.OnNext( new Monitored<IPacket>( TestHelper.Monitor, packet ) ) )
                .Returns( Task.Delay( 0 ) );
            topicEvaluator.Setup( e => e.Matches( It.IsAny<string>(), It.IsAny<string>() ) ).Returns( true );
            sessionRepository.Setup( r => r.ReadAll() ).Returns( sessions.AsQueryable() );

            connectionProvider
                .Setup( p => p.GetConnection( TestHelper.Monitor, It.Is<string>( s => s == subscribedClientId ) ) )
                .Returns( clientChannel.Object );

            ushort? packetId = (ushort?)new Random().Next( 0, ushort.MaxValue );
            Publish publish = new Publish( topic, MqttQualityOfService.ExactlyOnce, retain: false, duplicated: false, packetId: packetId )
            {
                Payload = Encoding.UTF8.GetBytes( "Publish Receiver Flow Test" )
            };

            Subject<Monitored<IPacket>> receiver = new Subject<Monitored<IPacket>>();
            Mock<IMqttChannel<IPacket>> channel = new Mock<IMqttChannel<IPacket>>();

            channel.Setup( c => c.ReceiverStream ).Returns( receiver );

            await flow.ExecuteAsync( TestHelper.Monitor, clientId, publish, channel.Object );

            publishSenderFlow.Verify( s => s.SendPublishAsync( TestHelper.Monitor, It.Is<string>( x => x == subscribedClientId ),
                It.Is<Publish>( p => p.Topic == publish.Topic &&
                    p.Payload.ToList().SequenceEqual( publish.Payload ) ),
                It.Is<IMqttChannel<IPacket>>( c => c == clientChannel.Object ), It.Is<PendingMessageStatus>( x => x == PendingMessageStatus.PendingToSend ) ), Times.Once );
            clientChannel.Verify( c => c.SendAsync( It.Is<Monitored<IPacket>>( p => p.Item is Publish ) ), Times.Never );
        }

        [Test]
        public async Task when_sending_publish_and_subscriber_with_qos2_send_publish_received_then_publish_is_not_re_transmitted()
        {
            string clientId = Guid.NewGuid().ToString();

            MqttConfiguration configuration = new MqttConfiguration
            {
                MaximumQualityOfService = MqttQualityOfService.ExactlyOnce,
                WaitTimeoutSecs = 2
            };
            Mock<IMqttTopicEvaluator> topicEvaluator = new Mock<IMqttTopicEvaluator>();
            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();
            Mock<IPublishSenderFlow> publishSenderFlow = new Mock<IPublishSenderFlow>();
            Mock<IRepository<RetainedMessage>> retainedRepository = new Mock<IRepository<RetainedMessage>>();
            Mock<IRepository<ClientSession>> sessionRepository = new Mock<IRepository<ClientSession>>();
            Mock<IRepository<ConnectionWill>> willRepository = new Mock<IRepository<ConnectionWill>>();

            sessionRepository.Setup( r => r.Read( It.IsAny<string>() ) )
                .Returns( new ClientSession( clientId )
                {
                    PendingMessages = new List<PendingMessage> { new PendingMessage() }
                } );

            IPacketIdProvider packetIdProvider = Mock.Of<IPacketIdProvider>();
            Subject<MqttUndeliveredMessage> undeliveredMessagesListener = new Subject<MqttUndeliveredMessage>();

            string topic = "foo/bar";

            ServerPublishReceiverFlow flow = new ServerPublishReceiverFlow( topicEvaluator.Object, connectionProvider.Object, publishSenderFlow.Object,
                retainedRepository.Object, sessionRepository.Object, willRepository.Object, packetIdProvider, undeliveredMessagesListener, configuration );

            string subscribedClientId = Guid.NewGuid().ToString();
            MqttQualityOfService requestedQoS = MqttQualityOfService.ExactlyOnce;
            List<ClientSession> sessions = new List<ClientSession> { new ClientSession (subscribedClientId, clean: false) {
                Subscriptions = new List<ClientSubscription> {
                    new ClientSubscription { ClientId = subscribedClientId, MaximumQualityOfService = requestedQoS, TopicFilter = topic }
                }
            }};

            Subject<Monitored<IPacket>> clientReceiver = new Subject<Monitored<IPacket>>();
            Subject<Monitored<IPacket>> clientSender = new Subject<Monitored<IPacket>>();
            Mock<IMqttChannel<IPacket>> clientChannel = new Mock<IMqttChannel<IPacket>>();

            clientSender.OfType<Publish>().Subscribe( p =>
            {
                clientReceiver.OnNext( new Monitored<IPacket>( TestHelper.Monitor, new PublishReceived( p.PacketId.Value ) ) );
            } );

            clientChannel.Setup( c => c.ReceiverStream ).Returns( clientReceiver );
            clientChannel.Setup( c => c.SenderStream ).Returns( clientSender );
            clientChannel.Setup( c => c.SendAsync( It.IsAny<Monitored<IPacket>>() ) )
                .Callback<IPacket>( packet => clientSender.OnNext( new Monitored<IPacket>( TestHelper.Monitor, packet ) ) )
                .Returns( Task.Delay( 0 ) );
            topicEvaluator.Setup( e => e.Matches( It.IsAny<string>(), It.IsAny<string>() ) ).Returns( true );
            sessionRepository.Setup( r => r.ReadAll() ).Returns( sessions.AsQueryable() );

            connectionProvider
                .Setup( p => p.GetConnection( TestHelper.Monitor, It.Is<string>( s => s == subscribedClientId ) ) )
                .Returns( clientChannel.Object );

            ushort? packetId = (ushort?)new Random().Next( 0, ushort.MaxValue );
            Publish publish = new Publish( topic, MqttQualityOfService.ExactlyOnce, retain: false, duplicated: false, packetId: packetId )
            {
                Payload = Encoding.UTF8.GetBytes( "Publish Receiver Flow Test" )
            };

            Subject<Monitored<IPacket>> receiver = new Subject<Monitored<IPacket>>();
            Mock<IMqttChannel<IPacket>> channel = new Mock<IMqttChannel<IPacket>>();

            channel.Setup( c => c.ReceiverStream ).Returns( receiver );

            await flow.ExecuteAsync( TestHelper.Monitor, clientId, publish, channel.Object );

            publishSenderFlow.Verify( s => s.SendPublishAsync( TestHelper.Monitor, It.Is<string>( x => x == subscribedClientId ),
                It.Is<Publish>( p => p.Topic == publish.Topic &&
                    p.Payload.ToList().SequenceEqual( publish.Payload ) ),
                It.Is<IMqttChannel<IPacket>>( c => c == clientChannel.Object ), It.Is<PendingMessageStatus>( x => x == PendingMessageStatus.PendingToSend ) ), Times.Once );
            clientChannel.Verify( c => c.SendAsync( It.Is<Monitored<IPacket>>( p => p.Item is Publish ) ), Times.Never );
        }

        [Test]
        public async Task when_sending_publish_release_then_publish_complete_is_sent()
        {
            string clientId = Guid.NewGuid().ToString();

            MqttConfiguration configuration = Mock.Of<MqttConfiguration>();
            Mock<IMqttTopicEvaluator> topicEvaluator = new Mock<IMqttTopicEvaluator>();
            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();
            Mock<IPublishSenderFlow> publishSenderFlow = new Mock<IPublishSenderFlow>();
            IRepository<RetainedMessage> retainedRepository = Mock.Of<IRepository<RetainedMessage>>();
            Mock<IRepository<ClientSession>> sessionRepository = new Mock<IRepository<ClientSession>>();
            Mock<IRepository<ConnectionWill>> willRepository = new Mock<IRepository<ConnectionWill>>();

            sessionRepository.Setup( r => r.Read( It.IsAny<string>() ) )
                .Returns( new ClientSession( clientId )
                {
                    PendingMessages = new List<PendingMessage> { new PendingMessage() }
                } );

            IPacketIdProvider packetIdProvider = Mock.Of<IPacketIdProvider>();
            Subject<MqttUndeliveredMessage> undeliveredMessagesListener = new Subject<MqttUndeliveredMessage>();

            ServerPublishReceiverFlow flow = new ServerPublishReceiverFlow( topicEvaluator.Object, connectionProvider.Object, publishSenderFlow.Object,
                retainedRepository, sessionRepository.Object, willRepository.Object, packetIdProvider, undeliveredMessagesListener, configuration );

            ushort packetId = (ushort)new Random().Next( 0, ushort.MaxValue );
            PublishRelease publishRelease = new PublishRelease( packetId );

            Mock<IMqttChannel<IPacket>> channel = new Mock<IMqttChannel<IPacket>>();

            channel.Setup( c => c.IsConnected ).Returns( true );

            await flow.ExecuteAsync( TestHelper.Monitor, clientId, publishRelease, channel.Object );

            channel.Verify( c => c.SendAsync( It.Is<Monitored<IPacket>>( p => p.Item is PublishComplete && (p.Item as PublishComplete).PacketId == packetId ) ) );
        }

        [Test]
        public void when_sending_publish_to_a_system_topic_with_remote_client_then_fails()
        {
            string clientId = Guid.NewGuid().ToString();

            Mock<MqttConfiguration> configuration = new Mock<MqttConfiguration>();
            Mock<IMqttTopicEvaluator> topicEvaluator = new Mock<IMqttTopicEvaluator>();
            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();
            Mock<IPublishSenderFlow> publishSenderFlow = new Mock<IPublishSenderFlow>();
            Mock<IRepository<RetainedMessage>> retainedRepository = new Mock<IRepository<RetainedMessage>>();
            Mock<IRepository<ClientSession>> sessionRepository = new Mock<IRepository<ClientSession>>();
            Mock<IRepository<ConnectionWill>> willRepository = new Mock<IRepository<ConnectionWill>>();

            sessionRepository.Setup( r => r.Read( It.IsAny<string>() ) )
                .Returns( new ClientSession( clientId )
                {
                    PendingMessages = new List<PendingMessage> { new PendingMessage() }
                } );

            IPacketIdProvider packetIdProvider = Mock.Of<IPacketIdProvider>();
            Subject<MqttUndeliveredMessage> undeliveredMessagesListener = new Subject<MqttUndeliveredMessage>();

            string systemTopic = "$SYS/foo";

            ServerPublishReceiverFlow flow = new ServerPublishReceiverFlow( topicEvaluator.Object, connectionProvider.Object,
                publishSenderFlow.Object, retainedRepository.Object, sessionRepository.Object, willRepository.Object,
                packetIdProvider, undeliveredMessagesListener, configuration.Object );

            Publish publish = new Publish( systemTopic, MqttQualityOfService.AtMostOnce, retain: false, duplicated: false )
            {
                Payload = Encoding.UTF8.GetBytes( "Publish Receiver Flow Test" )
            };

            Subject<Monitored<IPacket>> receiver = new Subject<Monitored<IPacket>>();
            Mock<IMqttChannel<IPacket>> channel = new Mock<IMqttChannel<IPacket>>();

            channel.Setup( c => c.ReceiverStream ).Returns( receiver );

            AggregateException ex = Assert.Throws<AggregateException>( () => flow.ExecuteAsync( TestHelper.Monitor, clientId, publish, channel.Object ).Wait() );

            Assert.NotNull( ex );
            Assert.NotNull( ex.InnerException );
            Assert.True( ex.InnerException is MqttException );
            ex.InnerException.Message.Should()
                .Be(
                    ServerProperties.ServerPublishReceiverFlow_SystemMessageNotAllowedForClient );
        }

        [Test]
        public async Task when_sending_publish_to_a_system_topic_with_private_client_then_succeeds()
        {
            string clientId = Guid.NewGuid().ToString();

            Mock<MqttConfiguration> configuration = new Mock<MqttConfiguration>();
            Mock<IMqttTopicEvaluator> topicEvaluator = new Mock<IMqttTopicEvaluator>();
            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();
            Mock<IPublishSenderFlow> publishSenderFlow = new Mock<IPublishSenderFlow>();
            Mock<IRepository<RetainedMessage>> retainedRepository = new Mock<IRepository<RetainedMessage>>();
            Mock<IRepository<ClientSession>> sessionRepository = new Mock<IRepository<ClientSession>>();
            Mock<IRepository<ConnectionWill>> willRepository = new Mock<IRepository<ConnectionWill>>();

            connectionProvider.Setup( p => p.PrivateClients )
                .Returns( new[] { clientId } );

            sessionRepository.Setup( r => r.Read( It.IsAny<string>() ) )
                .Returns( new ClientSession( clientId )
                {
                    PendingMessages = new List<PendingMessage> { new PendingMessage() }
                } );

            IPacketIdProvider packetIdProvider = Mock.Of<IPacketIdProvider>();
            Subject<MqttUndeliveredMessage> undeliveredMessagesListener = new Subject<MqttUndeliveredMessage>();

            string systemTopic = "$SYS/foo";

            ServerPublishReceiverFlow flow = new ServerPublishReceiverFlow( topicEvaluator.Object, connectionProvider.Object,
                publishSenderFlow.Object, retainedRepository.Object, sessionRepository.Object, willRepository.Object,
                packetIdProvider, undeliveredMessagesListener, configuration.Object );

            Publish publish = new Publish( systemTopic, MqttQualityOfService.AtMostOnce, retain: false, duplicated: false )
            {
                Payload = Encoding.UTF8.GetBytes( "Publish Receiver Flow Test" )
            };

            Subject<Monitored<IPacket>> receiver = new Subject<Monitored<IPacket>>();
            Mock<IMqttChannel<IPacket>> channel = new Mock<IMqttChannel<IPacket>>();

            channel.Setup( c => c.ReceiverStream ).Returns( receiver );

            await flow.ExecuteAsync( TestHelper.Monitor, clientId, publish, channel.Object );

            retainedRepository.Verify( r => r.Create( It.IsAny<RetainedMessage>() ), Times.Never );
            channel.Verify( c => c.SendAsync( It.IsAny<Monitored<IPacket>>() ), Times.Never );
            publishSenderFlow.Verify( s => s.SendPublishAsync( TestHelper.Monitor, It.IsAny<string>(),
               It.IsAny<Publish>(), It.IsAny<IMqttChannel<IPacket>>(), It.IsAny<PendingMessageStatus>() ), Times.Never );
        }
    }
}
