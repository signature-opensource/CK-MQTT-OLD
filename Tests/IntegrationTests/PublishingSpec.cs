using CK.Core;
using CK.MQTT;
using CK.MQTT.Sdk;
using CK.MQTT.Sdk.Packets;
using FluentAssertions;
using IntegrationTests.Context;
using IntegrationTests.Messages;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using static CK.Testing.MonitorTestHelper;

namespace IntegrationTests
{
    public abstract class PublishingSpec : ConnectedContext
    {
        [TestCase( 100 )]
        [TestCase( 500 )]
        [TestCase( 1000 )]
        public async Task when_publish_messages_with_qos0_then_succeeds( int count )
        {
            (IMqttClient client, IActivityMonitor m) = await GetClientAsync();
            using( client )
            {
                string topic = Guid.NewGuid().ToString();
                List<Task> tasks = new List<Task>();

                for( int i = 1; i <= count; i++ )
                {
                    TestMessage testMessage = GetTestMessage( i );
                    MqttApplicationMessage message = new MqttApplicationMessage( topic, Serializer.Serialize( testMessage ) );

                    tasks.Add( client.PublishAsync( m, message, MqttQualityOfService.AtMostOnce ) );
                }

                await Task.WhenAll( tasks );

                Assert.True( client.CheckConnection( m ) );
            }
        }

        [TestCase( 100 )]
        [TestCase( 500 )]
        [TestCase( 1000 )]
        public async Task when_publish_messages_with_qos1_then_succeeds( int count )
        {
            (IMqttClient client, IActivityMonitor m) = await GetClientAsync();
            using( client )
            {
                string topic = Guid.NewGuid().ToString();
                List<Task> tasks = new List<Task>();

                int publishAckPackets = 0;

                (client as MqttClientImpl)
                   .Channel
                   .ReceiverStream
                   .Subscribe( packet =>
                   {
                       if( packet.Item is PublishAck )
                       {
                           Interlocked.Increment( ref publishAckPackets );
                       }
                   } );

                for( int i = 1; i <= count; i++ )
                {
                    TestMessage testMessage = GetTestMessage( i );
                    MqttApplicationMessage message = new MqttApplicationMessage( topic, Serializer.Serialize( testMessage ) );

                    tasks.Add( client.PublishAsync( m, message, MqttQualityOfService.AtLeastOnce ) );
                }

                await Task.WhenAll( tasks );

                Assert.True( client.CheckConnection( m ) );
                Assert.True( publishAckPackets >= count );
            }
        }

        [TestCase( 100 )]
        [TestCase( 200 )]
        public async Task when_publish_messages_with_qos2_then_succeeds( int count )
        {
            (IMqttClient client, IActivityMonitor m) = await GetClientAsync();
            using( client )
            {
                string topic = Guid.NewGuid().ToString();
                List<Task> tasks = new List<Task>();

                int publishReceivedPackets = 0;
                int publishCompletePackets = 0;

                (client as MqttClientImpl)
                    .Channel
                    .ReceiverStream
                    .Subscribe( packet =>
                    {
                        if( packet.Item is PublishReceived )
                        {
                            Interlocked.Increment( ref publishReceivedPackets );
                        }
                        else if( packet.Item is PublishComplete )
                        {
                            Interlocked.Increment( ref publishCompletePackets );
                        }
                    } );

                for( int i = 1; i <= count; i++ )
                {
                    TestMessage testMessage = GetTestMessage( i );
                    MqttApplicationMessage message = new MqttApplicationMessage( topic, Serializer.Serialize( testMessage ) );

                    tasks.Add( client.PublishAsync( m, message, MqttQualityOfService.ExactlyOnce ) );
                }

                await Task.WhenAll( tasks );

                Assert.True( client.CheckConnection( m ) );
                Assert.True( publishReceivedPackets >= count );
                Assert.True( publishCompletePackets >= count );
            }
        }

        [TestCase( 100 )]
        [TestCase( 200 )]
        public async Task when_publish_message_to_topic_then_message_is_dispatched_to_subscribers( int count )
        {
            string guid = Guid.NewGuid().ToString();
            string topicFilter = guid + "/#";
            string topic = guid;
            (IMqttClient publisher, IActivityMonitor mPub) = await GetClientAsync();
            (IMqttClient subscriber1, IActivityMonitor mSub1) = await GetClientAsync();
            (IMqttClient subscriber2, IActivityMonitor mSub2) = await GetClientAsync();
            using( publisher )
            using( subscriber1 )
            using( subscriber2 )
            {
                ManualResetEventSlim subscriber1Done = new ManualResetEventSlim();
                ManualResetEventSlim subscriber2Done = new ManualResetEventSlim();
                int subscriber1Received = 0;
                int subscriber2Received = 0;

                await subscriber1.SubscribeAsync( mSub1, topicFilter, MqttQualityOfService.AtMostOnce );
                await subscriber2.SubscribeAsync( mSub2, topicFilter, MqttQualityOfService.AtMostOnce );

                subscriber1.MessageStream
                    .Subscribe( m =>
                    {
                        if( m.Item.Topic == topic )
                        {
                            Interlocked.Increment( ref subscriber1Received );

                            if( subscriber1Received == count )
                                subscriber1Done.Set();
                        }
                    } );

                subscriber2.MessageStream
                    .Subscribe( m =>
                    {
                        if( m.Item.Topic == topic )
                        {
                            Interlocked.Increment( ref subscriber2Received );

                            if( subscriber2Received == count )
                                subscriber2Done.Set();
                        }
                    } );

                List<Task> tasks = new List<Task>();

                for( int i = 1; i <= count; i++ )
                {
                    TestMessage testMessage = GetTestMessage( i );
                    MqttApplicationMessage message = new MqttApplicationMessage( topic, Serializer.Serialize( testMessage ) );

                    tasks.Add( publisher.PublishAsync( mPub, message, MqttQualityOfService.AtMostOnce ) );
                }

                await Task.WhenAll( tasks );

                bool completed = WaitHandle.WaitAll( new WaitHandle[] { subscriber1Done.WaitHandle, subscriber2Done.WaitHandle }, TimeSpan.FromSeconds( Configuration.WaitTimeoutSecs ) );

                count.Should().Be( subscriber1Received );
                count.Should().Be( subscriber2Received );
                Assert.True( completed );

                await subscriber1.UnsubscribeAsync( mSub1, topicFilter );
                await subscriber2.UnsubscribeAsync( mSub2, topicFilter );
            }
        }

        [TestCase( 100 )]
        [TestCase( 500 )]
        public async Task when_publish_message_to_topic_and_there_is_no_subscribers_then_server_notifies( int count )
        {
            string topic = Guid.NewGuid().ToString();
            (IMqttClient publisher, IActivityMonitor m) = await GetClientAsync();
            using( publisher )
            {
                int topicsNotSubscribedCount = 0;
                ManualResetEventSlim topicsNotSubscribedDone = new ManualResetEventSlim();

                Server.MessageUndelivered += ( sender, e ) =>
                {
                    Interlocked.Increment( ref topicsNotSubscribedCount );

                    if( topicsNotSubscribedCount == count )
                    {
                        topicsNotSubscribedDone.Set();
                    }
                };

                List<Task> tasks = new List<Task>();

                for( int i = 1; i <= count; i++ )
                {
                    TestMessage testMessage = GetTestMessage( i );
                    MqttApplicationMessage message = new MqttApplicationMessage( topic, Serializer.Serialize( testMessage ) );

                    tasks.Add( publisher.PublishAsync( m, message, MqttQualityOfService.AtMostOnce ) );
                }

                await Task.WhenAll( tasks );

                bool success = topicsNotSubscribedDone.Wait( TimeSpan.FromSeconds( KeepAliveSecs * 2 ) );

                topicsNotSubscribedCount.Should().Be( count );
                Assert.True( success );

            }
        }

        [TestCase( 100 )]
        [TestCase( 200 )]
        public async Task when_publish_message_to_topic_and_expect_reponse_to_other_topic_then_succeeds( int count )
        {
            string guid = Guid.NewGuid().ToString();
            string requestTopic = guid;
            string responseTopic = guid + "/response";

            (IMqttClient publisher, IActivityMonitor mPub) = await GetClientAsync();
            (IMqttClient subscriber, IActivityMonitor mSub) = await GetClientAsync();
            using( publisher )
            using( subscriber )
            {

                ManualResetEventSlim subscriberDone = new ManualResetEventSlim();
                int subscriberReceived = 0;

                await subscriber.SubscribeAsync( mSub, requestTopic, MqttQualityOfService.AtMostOnce );
                await publisher.SubscribeAsync( mPub, responseTopic, MqttQualityOfService.AtMostOnce );

                subscriber.MessageStream
                    .Subscribe( async m =>
                    {
                        if( m.Item.Topic == requestTopic )
                        {
                            RequestMessage request = Serializer.Deserialize<RequestMessage>( m.Item.Payload );
                            ResponseMessage response = GetResponseMessage( request );
                            MqttApplicationMessage message = new MqttApplicationMessage( responseTopic, Serializer.Serialize( response ) );

                            await subscriber.PublishAsync( mSub, message, MqttQualityOfService.AtMostOnce );
                        }
                    } );

                publisher.MessageStream
                    .Subscribe( m =>
                    {
                        if( m.Item.Topic == responseTopic )
                        {
                            Interlocked.Increment( ref subscriberReceived );

                            if( subscriberReceived == count )
                                subscriberDone.Set();
                        }
                    } );

                List<Task> tasks = new List<Task>();

                for( int i = 1; i <= count; i++ )
                {
                    RequestMessage request = GetRequestMessage();
                    MqttApplicationMessage message = new MqttApplicationMessage( requestTopic, Serializer.Serialize( request ) );

                    tasks.Add( publisher.PublishAsync( mPub, message, MqttQualityOfService.AtMostOnce ) );
                }

                await Task.WhenAll( tasks );

                bool completed = subscriberDone.Wait( TimeSpan.FromSeconds( Configuration.WaitTimeoutSecs ) );

                subscriberReceived.Should().Be( count );
                Assert.True( completed );

                await subscriber.UnsubscribeAsync( mSub, requestTopic );
                await publisher.UnsubscribeAsync( mPub, responseTopic );
            }
        }

        [TestCase( 100 )]
        [TestCase( 200 )]
        public async Task when_publish_with_qos0_and_subscribe_with_same_client_intensively_then_succeeds( int count )
        {
            (IMqttClient client, IActivityMonitor m) = await GetClientAsync();
            using( client )
            {
                List<Task> tasks = new List<Task>();
                for( int i = 1; i <= count; i++ )
                {
                    Task<Task> subscribePublishTask = client
                        .SubscribeAsync( m, Guid.NewGuid().ToString(), MqttQualityOfService.AtMostOnce )
                        .ContinueWith( t => client.PublishAsync( m, new MqttApplicationMessage( Guid.NewGuid().ToString(), Encoding.UTF8.GetBytes( "Foo Message" ) ), MqttQualityOfService.AtMostOnce ) );

                    tasks.Add( subscribePublishTask );
                }

                await Task.WhenAll( tasks );

                Assert.True( client.CheckConnection( m ) );
            }
        }

        [TestCase( 100 )]
        [TestCase( 200 )]
        public async Task when_publish_with_qos1_and_subscribe_with_same_client_intensively_then_succeeds( int count )
        {
            (IMqttClient client, IActivityMonitor m) = await GetClientAsync();
            using( client )
            {
                List<Task> tasks = new List<Task>();

                for( int i = 1; i <= count; i++ )
                {
                    Task<Task> subscribePublishTask = client
                        .SubscribeAsync( m, Guid.NewGuid().ToString(), MqttQualityOfService.AtLeastOnce )
                        .ContinueWith( t => client.PublishAsync( m, new MqttApplicationMessage( Guid.NewGuid().ToString(), Encoding.UTF8.GetBytes( "Foo Message" ) ), MqttQualityOfService.AtLeastOnce ) );

                    tasks.Add( subscribePublishTask );
                }

                await Task.WhenAll( tasks );

                Assert.True( client.CheckConnection( m ) );
            }
        }

        [TestCase( 100 )]
        [TestCase( 200 )]
        public async Task when_publish_with_qos2_and_subscribe_with_same_client_intensively_then_succeeds( int count )
        {
            (IMqttClient client, IActivityMonitor m) = await GetClientAsync();
            using( client )
            {

                List<Task> tasks = new List<Task>();

                for( int i = 1; i <= count; i++ )
                {
                    Task<Task> subscribePublishTask = client
                            .SubscribeAsync( m, Guid.NewGuid().ToString(), MqttQualityOfService.ExactlyOnce )
                            .ContinueWith( t => client.PublishAsync( m, new MqttApplicationMessage( Guid.NewGuid().ToString(), Encoding.UTF8.GetBytes( "Foo Message" ) ), MqttQualityOfService.ExactlyOnce ), TaskContinuationOptions.OnlyOnRanToCompletion );

                    tasks.Add( subscribePublishTask );
                }

                await Task.WhenAll( tasks );

                Assert.True( client.CheckConnection( m ) );
            }
        }

        [Test]
        public async Task when_publish_system_messages_then_fails_and_server_disconnects_client()
        {
            (IMqttClient client, IActivityMonitor m) = await GetClientAsync();
            using( client )
            {
                string topic = "$SYS/" + Guid.NewGuid().ToString();
                MqttApplicationMessage message = new MqttApplicationMessage( topic, Encoding.UTF8.GetBytes( "Foo Message" ) );

                ManualResetEventSlim clientDisconnectedEvent = new ManualResetEventSlim();

                client.Disconnected += ( sender, e ) =>
                {
                    if( e.Reason == DisconnectedReason.RemoteDisconnected )
                    {
                        clientDisconnectedEvent.Set();
                    }
                };

                await client.PublishAsync( m, message, MqttQualityOfService.ExactlyOnce );

                bool clientRemoteDisconnected = clientDisconnectedEvent.Wait( 2000 );

                Assert.True( clientRemoteDisconnected );
            }
        }

        [Test]
        public async Task when_publish_without_clean_session_then_pending_messages_are_sent_when_reconnect()
        {
            Assume.That( false, "To investigate." );
            ManualResetEventSlim client1Done = new ManualResetEventSlim();
            int client1Received = 0;
            (IMqttClient client1, IActivityMonitor m1) = await GetClientAsync();
            (IMqttClient client2, IActivityMonitor m2) = await GetClientAsync();
            using( client1 )
            using( client2 )
            {
                string client2Id = client2.ClientId;
                ManualResetEventSlim client2Done = new ManualResetEventSlim();
                int client2Received = 0;

                string topic = "topic/foo/bar";
                int messagesBeforeDisconnect = 3;
                int messagesAfterReconnect = 2;

                await client1.SubscribeAsync( m1, topic, MqttQualityOfService.AtLeastOnce );
                await client2.SubscribeAsync( m2, topic, MqttQualityOfService.AtLeastOnce );

                IDisposable subscription1 = client1
                    .MessageStream
                    .Where( m => m.Item.Topic == topic )
                    .Subscribe( m =>
                    {
                        Interlocked.Increment( ref client1Received );

                        if( client1Received == messagesBeforeDisconnect )
                            client1Done.Set();
                    } );

                IDisposable subscription2 = client2
                    .MessageStream
                    .Where( m => m.Item.Topic == topic )
                    .Subscribe( m =>
                    {
                        Interlocked.Increment( ref client2Received );

                        if( client2Received == messagesBeforeDisconnect )
                            client2Done.Set();
                    } );

                for( int i = 1; i <= messagesBeforeDisconnect; i++ )
                {
                    TestMessage testMessage = GetTestMessage( i );
                    MqttApplicationMessage message = new MqttApplicationMessage( topic, Serializer.Serialize( testMessage ) );

                    await client1.PublishAsync( TestHelper.Monitor, message, MqttQualityOfService.AtLeastOnce, retain: false );
                }

                bool completed = WaitHandle.WaitAll( new WaitHandle[] { client1Done.WaitHandle, client2Done.WaitHandle }, TimeSpan.FromSeconds( Configuration.WaitTimeoutSecs ) );

                Assert.True( completed, $"Messages before disconnect weren't all received. Client 1 received: {client1Received}, Client 2 received: {client2Received}" );
                messagesBeforeDisconnect.Should().Be( client1Received );
                messagesBeforeDisconnect.Should().Be( client2Received );

                await client2.DisconnectAsync( TestHelper.Monitor );

                subscription1.Dispose();
                client1Received = 0;
                client1Done.Reset();
                subscription2.Dispose();
                client2Received = 0;
                client2Done.Reset();

                int client1OldMessagesReceived = 0;
                int client2OldMessagesReceived = 0;

                subscription1 = client1
                    .MessageStream
                    .Where( m => m.Item.Topic == topic )
                    .Subscribe( m =>
                    {
                        TestMessage testMessage = Serializer.Deserialize<TestMessage>( m.Item.Payload );

                        if( testMessage.Id > messagesBeforeDisconnect )
                            Interlocked.Increment( ref client1Received );
                        else
                            Interlocked.Increment( ref client1OldMessagesReceived );

                        if( client1Received == messagesAfterReconnect )
                            client1Done.Set();
                    } );

                subscription2 = client2
                    .MessageStream
                    .Where( m => m.Item.Topic == topic )
                    .Subscribe( m =>
                    {
                        TestMessage testMessage = Serializer.Deserialize<TestMessage>( m.Item.Payload );

                        if( testMessage.Id > messagesBeforeDisconnect )
                            Interlocked.Increment( ref client2Received );
                        else
                            Interlocked.Increment( ref client2Received );

                        if( client2Received == messagesAfterReconnect )
                            client2Done.Set();
                    } );

                for( int i = messagesBeforeDisconnect + 1; i <= messagesBeforeDisconnect + messagesAfterReconnect; i++ )
                {
                    TestMessage testMessage = GetTestMessage( i );
                    MqttApplicationMessage message = new MqttApplicationMessage( topic, Serializer.Serialize( testMessage ) );

                    await client1.PublishAsync( TestHelper.Monitor, message, MqttQualityOfService.AtLeastOnce, retain: false );
                }

                await client2.ConnectAsync( TestHelper.Monitor, new MqttClientCredentials( client2Id ), cleanSession: false );

                completed = WaitHandle.WaitAll( new WaitHandle[] { client1Done.WaitHandle, client2Done.WaitHandle }, TimeSpan.FromSeconds( Configuration.WaitTimeoutSecs ) );

                Assert.True( completed, $"Messages after re connect weren't all received. Client 1 received: {client1Received}, Client 2 received: {client2Received}" );
                messagesAfterReconnect.Should().Be( client1Received );
                messagesAfterReconnect.Should().Be( client2Received );
                0.Should().Be( client1OldMessagesReceived );
                0.Should().Be( client2OldMessagesReceived );

                await client1.UnsubscribeAsync( m1, topic );
                await client2.UnsubscribeAsync( m2, topic );
            }
        }

        [TestCase( 200 )]
        public async Task when_publish_with_client_with_session_present_then_subscriptions_are_re_used( int count )
        {
            Assume.That( false, "To investigate." );
            string topic = "topic/foo/bar";

            (IMqttClient publisher, IActivityMonitor mPub) = await GetClientAsync();
            (IMqttClient subscriber, IActivityMonitor mSub) = await GetClientAsync();
            using( publisher )
            using( subscriber )
            {
                string subscriberId = subscriber.ClientId;

                ManualResetEventSlim subscriberDone = new ManualResetEventSlim();
                int subscriberReceived = 0;

                await subscriber
                    .SubscribeAsync( mSub, topic, MqttQualityOfService.AtMostOnce );

                subscriber
                    .MessageStream
                    .Where( m => m.Item.Topic == topic )
                    .Subscribe( m =>
                    {
                        Interlocked.Increment( ref subscriberReceived );

                        if( subscriberReceived == count )
                        {
                            subscriberDone.Set();
                        }
                    } );

                await subscriber.DisconnectAsync( mSub );

                SessionState sessionState = await subscriber.ConnectAsync( mSub, new MqttClientCredentials( subscriberId ), cleanSession: false );

                subscriber
                    .MessageStream
                    .Where( m => m.Item.Topic == topic )
                    .Subscribe( m =>
                    {
                        Interlocked.Increment( ref subscriberReceived );
                        if( subscriberReceived == count )
                        {
                            subscriberDone.Set();
                        }
                    } );

                List<Task> tasks = new List<Task>();

                for( int i = 1; i <= count; i++ )
                {
                    TestMessage testMessage = GetTestMessage( i );
                    MqttApplicationMessage message = new MqttApplicationMessage( topic, Serializer.Serialize( testMessage ) );

                    tasks.Add( publisher.PublishAsync( mPub, message, MqttQualityOfService.AtMostOnce ) );
                }

                await Task.WhenAll( tasks );

                bool completed = subscriberDone.Wait( TimeSpan.FromSeconds( Configuration.WaitTimeoutSecs * 4 ) );

                Assert.True( completed );
                SessionState.SessionPresent.Should().Be( sessionState );
                count.Should().Be( subscriberReceived );

                await subscriber.UnsubscribeAsync( mSub, topic );
            }
        }

        [TestCase( 100 )]
        [TestCase( 500 )]
        [TestCase( 1000 )]
        public async Task when_publish_with_client_with_session_clared_then_subscriptions_are_not_re_used( int count )
        {
            CleanSession = true;

            string topic = "topic/foo/bar";

            (IMqttClient publisher, IActivityMonitor mPub) = await GetClientAsync();
            (IMqttClient subscriber, IActivityMonitor mSub) = await GetClientAsync();
            using( publisher )
            using( subscriber )
            {
                string subscriberId = subscriber.ClientId;

                ManualResetEventSlim subscriberDone = new ManualResetEventSlim();
                int subscriberReceived = 0;

                await subscriber
                    .SubscribeAsync( mSub, topic, MqttQualityOfService.AtMostOnce );

                subscriber
                    .MessageStream
                    .Where( m => m.Item.Topic == topic )
                    .Subscribe( m =>
                    {
                        Interlocked.Increment( ref subscriberReceived );

                        if( subscriberReceived == count )
                            subscriberDone.Set();
                    } );

                await subscriber.DisconnectAsync( mSub );
                SessionState sessionState = await subscriber.ConnectAsync( mSub, new MqttClientCredentials( subscriberId ), cleanSession: true );

                List<Task> tasks = new List<Task>();

                for( int i = 1; i <= count; i++ )
                {
                    TestMessage testMessage = GetTestMessage( i );
                    MqttApplicationMessage message = new MqttApplicationMessage( topic, Serializer.Serialize( testMessage ) );

                    tasks.Add( publisher.PublishAsync( mPub, message, MqttQualityOfService.AtMostOnce ) );
                }

                await Task.WhenAll( tasks );

                bool completed = subscriberDone.Wait( TimeSpan.FromSeconds( Configuration.WaitTimeoutSecs ) );

                Assert.False( completed );
                SessionState.CleanSession.Should().Be( sessionState );
                0.Should().Be( subscriberReceived );

                await subscriber.UnsubscribeAsync( mSub, topic );

            }
        }

        [Test]
        public async Task when_publish_messages_and_client_disconnects_then_message_stream_is_reset()
        {
            string topic = Guid.NewGuid().ToString();

            (IMqttClient publisher, IActivityMonitor mPub) = await GetClientAsync();
            (IMqttClient subscriber, IActivityMonitor mSub) = await GetClientAsync();
            using( publisher )
            using( subscriber )
            {
                string subscriberId = subscriber.ClientId;

                int goal = default;
                ManualResetEventSlim goalAchieved = new ManualResetEventSlim();
                int received = 0;

                await subscriber.SubscribeAsync( mSub, topic, MqttQualityOfService.AtMostOnce );

                subscriber
                    .MessageStream
                    .Subscribe( m =>
                    {
                        if( m.Item.Topic == topic )
                        {
                            Interlocked.Increment( ref received );

                            if( received == goal )
                                goalAchieved.Set();
                        }
                    } );

                goal = 5;

                List<Task> tasks = new List<Task>();

                for( int i = 1; i <= goal; i++ )
                {
                    TestMessage testMessage = GetTestMessage( i );
                    MqttApplicationMessage message = new MqttApplicationMessage( topic, Serializer.Serialize( testMessage ) );

                    tasks.Add( publisher.PublishAsync( mPub, message, MqttQualityOfService.AtMostOnce ) );
                }

                await Task.WhenAll( tasks );

                bool completed = goalAchieved.Wait( TimeSpan.FromSeconds( Configuration.WaitTimeoutSecs ) );

                Assert.True( completed );
                goal.Should().Be( received );

                await subscriber.DisconnectAsync( mSub );

                goal = 3;
                goalAchieved.Reset();
                received = 0;
                completed = false;

                await subscriber.ConnectAsync( TestHelper.Monitor, new MqttClientCredentials( subscriberId ), cleanSession: false );

                for( int i = 1; i <= goal; i++ )
                {
                    TestMessage testMessage = GetTestMessage( i );
                    MqttApplicationMessage message = new MqttApplicationMessage( topic, Serializer.Serialize( testMessage ) );

                    tasks.Add( publisher.PublishAsync( mPub, message, MqttQualityOfService.AtMostOnce ) );
                }

                completed = goalAchieved.Wait( TimeSpan.FromSeconds( Configuration.WaitTimeoutSecs ) );

                Assert.False( completed );
                0.Should().Be( received );

                await subscriber.UnsubscribeAsync( mSub, topic );

            }
        }

        TestMessage GetTestMessage( int id )
        {
            return new TestMessage
            {
                Id = id,
                Name = string.Concat( "Message ", Guid.NewGuid().ToString().Substring( 0, 4 ) ),
                Value = new Random().Next()
            };
        }

        RequestMessage GetRequestMessage()
        {
            return new RequestMessage
            {
                Id = Guid.NewGuid(),
                Name = string.Concat( "Request ", Guid.NewGuid().ToString().Substring( 0, 4 ) ),
                Date = DateTime.Now,
                Content = new byte[30]
            };
        }

        ResponseMessage GetResponseMessage( RequestMessage request )
        {
            return new ResponseMessage
            {
                Name = request.Name,
                Ok = true
            };
        }
    }
}
