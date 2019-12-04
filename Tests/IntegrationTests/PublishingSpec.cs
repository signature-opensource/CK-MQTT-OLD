using IntegrationTests.Context;
using IntegrationTests.Messages;
using System;
using System.Collections.Generic;
using CK.MQTT;
using CK.MQTT.Sdk;
using CK.MQTT.Sdk.Packets;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace IntegrationTests
{
    public class PublishingSpec : ConnectedContext, IDisposable
    {
        IMqttServer server;

        [SetUp]
        public void SetUp()
        {
            server = GetServerAsync().Result;
        }

        [TearDown]
        public void TearDown()
        {
            server?.Dispose();
        }

        public PublishingSpec()
            : base( keepAliveSecs: 1 )
        {
        }

        [TestCase( 100 )]
        [TestCase( 500 )]
        [TestCase( 1000 )]
        public async Task when_publish_messages_with_qos0_then_succeeds(int count)
        {
            var client = await GetClientAsync();
            var topic = Guid.NewGuid().ToString();
            var tasks = new List<Task>();

            for( var i = 1; i <= count; i++ )
            {
                var testMessage = GetTestMessage( i );
                var message = new MqttApplicationMessage( topic, Serializer.Serialize( testMessage ) );

                tasks.Add( client.PublishAsync( message, MqttQualityOfService.AtMostOnce ) );
            }

            await Task.WhenAll( tasks );

            Assert.True( client.IsConnected );

            client.Dispose();
        }

        [TestCase( 100 )]
        [TestCase( 500 )]
        [TestCase( 1000 )]
        public async Task when_publish_messages_with_qos1_then_succeeds(int count)
        {
            var client = await GetClientAsync();
            var topic = Guid.NewGuid().ToString();
            var tasks = new List<Task>();

            var publishAckPackets = 0;

            (client as MqttClientImpl)
               .Channel
               .ReceiverStream
               .Subscribe( packet =>
               {
                   if( packet is PublishAck )
                   {
                       publishAckPackets++;
                   }
               } );

            for( var i = 1; i <= count; i++ )
            {
                var testMessage = GetTestMessage( i );
                var message = new MqttApplicationMessage( topic, Serializer.Serialize( testMessage ) );

                tasks.Add( client.PublishAsync( message, MqttQualityOfService.AtLeastOnce ) );
            }

            await Task.WhenAll( tasks );

            Assert.True( client.IsConnected );
            Assert.True( publishAckPackets >= count );

            client.Dispose();
        }

        [TestCase( 100 )]
        [TestCase( 200 )]
        public async Task when_publish_messages_with_qos2_then_succeeds(int count)
        {
            var client = await GetClientAsync();
            var topic = Guid.NewGuid().ToString();
            var tasks = new List<Task>();

            var publishReceivedPackets = 0;
            var publishCompletePackets = 0;

            (client as MqttClientImpl)
                .Channel
                .ReceiverStream
                .Subscribe( packet =>
                {
                    if( packet is PublishReceived )
                    {
                        publishReceivedPackets++;
                    }
                    else if( packet is PublishComplete )
                    {
                        publishCompletePackets++;
                    }
                } );

            for( var i = 1; i <= count; i++ )
            {
                var testMessage = GetTestMessage( i );
                var message = new MqttApplicationMessage( topic, Serializer.Serialize( testMessage ) );

                tasks.Add( client.PublishAsync( message, MqttQualityOfService.ExactlyOnce ) );
            }

            await Task.WhenAll( tasks );

            Assert.True( client.IsConnected );
            Assert.True( publishReceivedPackets >= count );
            Assert.True( publishCompletePackets >= count );

            client.Dispose();
        }

        [TestCase( 100 )]
        [TestCase( 200 )]
        public async Task when_publish_message_to_topic_then_message_is_dispatched_to_subscribers(int count)
        {

            var guid = Guid.NewGuid().ToString();
            var topicFilter = guid + "/#";
            var topic = guid;

            var publisher = await GetClientAsync();
            var subscriber1 = await GetClientAsync();
            var subscriber2 = await GetClientAsync();

            var subscriber1Done = new ManualResetEventSlim();
            var subscriber2Done = new ManualResetEventSlim();
            var subscriber1Received = 0;
            var subscriber2Received = 0;

            await subscriber1.SubscribeAsync( topicFilter, MqttQualityOfService.AtMostOnce )
                .ConfigureAwait( continueOnCapturedContext: false );
            await subscriber2.SubscribeAsync( topicFilter, MqttQualityOfService.AtMostOnce )
                .ConfigureAwait( continueOnCapturedContext: false );

            subscriber1.MessageStream
                .Subscribe( m =>
                {
                    if( m.Topic == topic )
                    {
                        subscriber1Received++;

                        if( subscriber1Received == count )
                            subscriber1Done.Set();
                    }
                } );

            subscriber2.MessageStream
                .Subscribe( m =>
                {
                    if( m.Topic == topic )
                    {
                        subscriber2Received++;

                        if( subscriber2Received == count )
                            subscriber2Done.Set();
                    }
                } );

            var tasks = new List<Task>();

            for( var i = 1; i <= count; i++ )
            {
                var testMessage = GetTestMessage( i );
                var message = new MqttApplicationMessage( topic, Serializer.Serialize( testMessage ) );

                tasks.Add( publisher.PublishAsync( message, MqttQualityOfService.AtMostOnce ) );
            }

            await Task.WhenAll( tasks );

            var completed = WaitHandle.WaitAll( new WaitHandle[] { subscriber1Done.WaitHandle, subscriber2Done.WaitHandle }, TimeSpan.FromSeconds( Configuration.WaitTimeoutSecs ) );

            count.Should().Be( subscriber1Received );
            count.Should().Be( subscriber2Received );
            Assert.True( completed );

            await subscriber1.UnsubscribeAsync( topicFilter )
                .ConfigureAwait( continueOnCapturedContext: false );
            await subscriber2.UnsubscribeAsync( topicFilter )
                .ConfigureAwait( continueOnCapturedContext: false );

            subscriber1.Dispose();
            subscriber2.Dispose();
            publisher.Dispose();
        }

        [TestCase( 100 )]
        [TestCase( 500 )]
        public async Task when_publish_message_to_topic_and_there_is_no_subscribers_then_server_notifies(int count)
        {
            var topic = Guid.NewGuid().ToString();
            var publisher = await GetClientAsync();
            var topicsNotSubscribedCount = 0;
            var topicsNotSubscribedDone = new ManualResetEventSlim();

            server.MessageUndelivered += ( sender, e ) =>
            {
                topicsNotSubscribedCount++;

                if( topicsNotSubscribedCount == count )
                {
                    topicsNotSubscribedDone.Set();
                }
            };

            var tasks = new List<Task>();

            for( var i = 1; i <= count; i++ )
            {
                var testMessage = GetTestMessage( i );
                var message = new MqttApplicationMessage( topic, Serializer.Serialize( testMessage ) );

                tasks.Add( publisher.PublishAsync( message, MqttQualityOfService.AtMostOnce ) );
            }

            await Task.WhenAll( tasks );

            var success = topicsNotSubscribedDone.Wait( TimeSpan.FromSeconds( keepAliveSecs * 2 ) );

            topicsNotSubscribedCount.Should().Be( count );
            Assert.True( success );

            publisher.Dispose();
        }

        [TestCase(100)]
        [TestCase(200 )]
        public async Task when_publish_message_to_topic_and_expect_reponse_to_other_topic_then_succeeds( int count)
        {
            var guid = Guid.NewGuid().ToString();
            var requestTopic = guid;
            var responseTopic = guid + "/response";

            var publisher = await GetClientAsync();
            var subscriber = await GetClientAsync();

            var subscriberDone = new ManualResetEventSlim();
            var subscriberReceived = 0;

            await subscriber.SubscribeAsync( requestTopic, MqttQualityOfService.AtMostOnce )
                .ConfigureAwait( continueOnCapturedContext: false );
            await publisher.SubscribeAsync( responseTopic, MqttQualityOfService.AtMostOnce )
                .ConfigureAwait( continueOnCapturedContext: false );

            subscriber.MessageStream
                .Subscribe( async m =>
                {
                    if( m.Topic == requestTopic )
                    {
                        var request = Serializer.Deserialize<RequestMessage>( m.Payload );
                        var response = GetResponseMessage( request );
                        var message = new MqttApplicationMessage( responseTopic, Serializer.Serialize( response ) );

                        await subscriber.PublishAsync( message, MqttQualityOfService.AtMostOnce )
                            .ConfigureAwait( continueOnCapturedContext: false );
                    }
                } );

            publisher.MessageStream
                .Subscribe( m =>
                {
                    if( m.Topic == responseTopic )
                    {
                        subscriberReceived++;

                        if( subscriberReceived == count )
                            subscriberDone.Set();
                    }
                } );

            var tasks = new List<Task>();

            for( var i = 1; i <= count; i++ )
            {
                var request = GetRequestMessage();
                var message = new MqttApplicationMessage( requestTopic, Serializer.Serialize( request ) );

                tasks.Add( publisher.PublishAsync( message, MqttQualityOfService.AtMostOnce ) );
            }

            await Task.WhenAll( tasks );

            var completed = subscriberDone.Wait( TimeSpan.FromSeconds( Configuration.WaitTimeoutSecs ) );

            subscriberReceived.Should().Be( count );
            Assert.True( completed );

            await subscriber.UnsubscribeAsync( requestTopic )
                .ConfigureAwait( continueOnCapturedContext: false );
            await publisher.UnsubscribeAsync( responseTopic )
                .ConfigureAwait( continueOnCapturedContext: false );

            subscriber.Dispose();
            publisher.Dispose();
        }

        [TestCase( 100 )]
        [TestCase( 200 )]
        public async Task when_publish_with_qos0_and_subscribe_with_same_client_intensively_then_succeeds(int count)
        {
            var client = await GetClientAsync();
            var tasks = new List<Task>();
            for( var i = 1; i <= count; i++ )
            {
                var subscribePublishTask = client
                    .SubscribeAsync( Guid.NewGuid().ToString(), MqttQualityOfService.AtMostOnce )
                    .ContinueWith( t => client.PublishAsync( new MqttApplicationMessage( Guid.NewGuid().ToString(), Encoding.UTF8.GetBytes( "Foo Message" ) ), MqttQualityOfService.AtMostOnce ) );

                tasks.Add( subscribePublishTask );
            }

            await Task.WhenAll( tasks );

            Assert.True( client.IsConnected );
        }

        [TestCase( 100 )]
        [TestCase( 200 )]
        public async Task when_publish_with_qos1_and_subscribe_with_same_client_intensively_then_succeeds(int count)
        {
            var client = await GetClientAsync();
            var tasks = new List<Task>();

            for( var i = 1; i <= count; i++ )
            {
                var subscribePublishTask = client
                    .SubscribeAsync( Guid.NewGuid().ToString(), MqttQualityOfService.AtLeastOnce )
                    .ContinueWith( t => client.PublishAsync( new MqttApplicationMessage( Guid.NewGuid().ToString(), Encoding.UTF8.GetBytes( "Foo Message" ) ), MqttQualityOfService.AtLeastOnce ) );

                tasks.Add( subscribePublishTask );
            }

            await Task.WhenAll( tasks );

            Assert.True( client.IsConnected );
        }

        [TestCase( 100 )]
        [TestCase( 200 )]
        public async Task when_publish_with_qos2_and_subscribe_with_same_client_intensively_then_succeeds(int count)
        {
            var client = await GetClientAsync();
            var tasks = new List<Task>();

            for( var i = 1; i <= count; i++ )
            {
                var subscribePublishTask = client
                        .SubscribeAsync( Guid.NewGuid().ToString(), MqttQualityOfService.ExactlyOnce )
                        .ContinueWith( t => client.PublishAsync( new MqttApplicationMessage( Guid.NewGuid().ToString(), Encoding.UTF8.GetBytes( "Foo Message" ) ), MqttQualityOfService.ExactlyOnce ), TaskContinuationOptions.OnlyOnRanToCompletion );

                tasks.Add( subscribePublishTask );
            }

            await Task.WhenAll( tasks );

            Assert.True( client.IsConnected );
        }

        [Test]
        public async Task when_publish_system_messages_then_fails_and_server_disconnects_client()
        {
            var client = await GetClientAsync();
            var topic = "$SYS/" + Guid.NewGuid().ToString();
            var message = new MqttApplicationMessage( topic, Encoding.UTF8.GetBytes( "Foo Message" ) );

            var clientDisconnectedEvent = new ManualResetEventSlim();

            client.Disconnected += ( sender, e ) =>
            {
                if( e.Reason == DisconnectedReason.RemoteDisconnected )
                {
                    clientDisconnectedEvent.Set();
                }
            };

            await client.PublishAsync( message, MqttQualityOfService.ExactlyOnce );

            var clientRemoteDisconnected = clientDisconnectedEvent.Wait( 2000 );

            Assert.True( clientRemoteDisconnected );

            client.Dispose();
        }

        [Test]
        public async Task when_publish_without_clean_session_then_pending_messages_are_sent_when_reconnect()
        {
            var client1 = await GetClientAsync();
            var client1Done = new ManualResetEventSlim();
            var client1Received = 0;

            var client2 = await GetClientAsync();
            var client2Id = client2.Id;
            var client2Done = new ManualResetEventSlim();
            var client2Received = 0;

            var topic = "topic/foo/bar";
            var messagesBeforeDisconnect = 3;
            var messagesAfterReconnect = 2;

            await client1.SubscribeAsync( topic, MqttQualityOfService.AtLeastOnce );
            await client2.SubscribeAsync( topic, MqttQualityOfService.AtLeastOnce );

            var subscription1 = client1
                .MessageStream
                .Where( m => m.Topic == topic )
                .Subscribe( m =>
                {
                    Interlocked.Increment( ref client1Received );

                    if( client1Received == messagesBeforeDisconnect )
                        client1Done.Set();
                } );

            var subscription2 = client2
                .MessageStream
                .Where( m => m.Topic == topic )
                .Subscribe( m =>
                {
                    Interlocked.Increment(ref client2Received);

                    if( client2Received == messagesBeforeDisconnect )
                        client2Done.Set();
                } );

            for( var i = 1; i <= messagesBeforeDisconnect; i++ )
            {
                var testMessage = GetTestMessage( i );
                var message = new MqttApplicationMessage( topic, Serializer.Serialize( testMessage ) );

                await client1.PublishAsync( message, MqttQualityOfService.AtLeastOnce, retain: false );
            }

            var completed = WaitHandle.WaitAll( new WaitHandle[] { client1Done.WaitHandle, client2Done.WaitHandle }, TimeSpan.FromSeconds( Configuration.WaitTimeoutSecs ) );

            Assert.True( completed, $"Messages before disconnect weren't all received. Client 1 received: {client1Received}, Client 2 received: {client2Received}" );
            messagesBeforeDisconnect.Should().Be( client1Received );
            messagesBeforeDisconnect.Should().Be( client2Received );

            await client2.DisconnectAsync();

            subscription1.Dispose();
            client1Received = 0;
            client1Done.Reset();
            subscription2.Dispose();
            client2Received = 0;
            client2Done.Reset();

            var client1OldMessagesReceived = 0;
            var client2OldMessagesReceived = 0;

            subscription1 = client1
                .MessageStream
                .Where( m => m.Topic == topic )
                .Subscribe( m =>
                {
                    var testMessage = Serializer.Deserialize<TestMessage>( m.Payload );

                    if( testMessage.Id > messagesBeforeDisconnect )
                        Interlocked.Increment( ref client1Received );
                    else
                        client1OldMessagesReceived++;

                    if( client1Received == messagesAfterReconnect )
                        client1Done.Set();
                } );

            subscription2 = client2
                .MessageStream
                .Where( m => m.Topic == topic )
                .Subscribe( m =>
                {
                    var testMessage = Serializer.Deserialize<TestMessage>( m.Payload );

                    if( testMessage.Id > messagesBeforeDisconnect )
                        client2Received++;
                    else
                        client2OldMessagesReceived++;

                    if( client2Received == messagesAfterReconnect )
                        client2Done.Set();
                } );

            for( var i = messagesBeforeDisconnect + 1; i <= messagesBeforeDisconnect + messagesAfterReconnect; i++ )
            {
                var testMessage = GetTestMessage( i );
                var message = new MqttApplicationMessage( topic, Serializer.Serialize( testMessage ) );

                await client1.PublishAsync( message, MqttQualityOfService.AtLeastOnce, retain: false );
            }

            await client2.ConnectAsync( new MqttClientCredentials( client2Id ), cleanSession: false );

            completed = WaitHandle.WaitAll( new WaitHandle[] { client1Done.WaitHandle, client2Done.WaitHandle }, TimeSpan.FromSeconds( Configuration.WaitTimeoutSecs ) );

            Assert.True( completed, $"Messages after re connect weren't all received. Client 1 received: {client1Received}, Client 2 received: {client2Received}" );
            messagesAfterReconnect.Should().Be( client1Received );
            messagesAfterReconnect.Should().Be( client2Received );
            0.Should().Be( client1OldMessagesReceived );
            0.Should().Be( client2OldMessagesReceived );

            await client1.UnsubscribeAsync( topic )
                .ConfigureAwait( continueOnCapturedContext: false );
            await client2.UnsubscribeAsync( topic )
                .ConfigureAwait( continueOnCapturedContext: false );

            client1.Dispose();
            client2.Dispose();
        }

        [TestCase( 100 )]
        public async Task when_publish_with_client_with_session_present_then_subscriptions_are_re_used(int count)
        {
            var topic = "topic/foo/bar";

            var publisher = await GetClientAsync();
            var subscriber = await GetClientAsync();
            var subscriberId = subscriber.Id;

            var subscriberDone = new ManualResetEventSlim();
            var subscriberReceived = 0;

            await subscriber
                .SubscribeAsync( topic, MqttQualityOfService.AtMostOnce )
                .ConfigureAwait( continueOnCapturedContext: false );

            subscriber
                .MessageStream
                .Where( m => m.Topic == topic )
                .Subscribe( m =>
                {
                    Interlocked.Increment( ref subscriberReceived );

                    if( subscriberReceived == count )
                        subscriberDone.Set();
                } );

            await subscriber.DisconnectAsync();

            var sessionState = await subscriber.ConnectAsync( new MqttClientCredentials( subscriberId ), cleanSession: false );

            subscriber
                .MessageStream
                .Where( m => m.Topic == topic )
                .Subscribe( m =>
                {
                    Interlocked.Increment( ref subscriberReceived );
                    if( subscriberReceived == count )
                        subscriberDone.Set();
                } );

            var tasks = new List<Task>();

            for( var i = 1; i <= count; i++ )
            {
                var testMessage = GetTestMessage( i );
                var message = new MqttApplicationMessage( topic, Serializer.Serialize( testMessage ) );

                tasks.Add( publisher.PublishAsync( message, MqttQualityOfService.AtMostOnce ) );
            }

            await Task.WhenAll( tasks );

            var completed = subscriberDone.Wait( TimeSpan.FromSeconds( Configuration.WaitTimeoutSecs ) );

            Assert.True( completed );
            SessionState.SessionPresent.Should().Be( sessionState );
            count.Should().Be( subscriberReceived );

            await subscriber.UnsubscribeAsync( topic )
                .ConfigureAwait( continueOnCapturedContext: false );

            subscriber.Dispose();
            publisher.Dispose();
        }

        [TestCase( 100 )]
        [TestCase( 500 )]
        [TestCase( 1000 )]
        public async Task when_publish_with_client_with_session_clared_then_subscriptions_are_not_re_used(int count)
        {
            CleanSession = true;

            var topic = "topic/foo/bar";

            var publisher = await GetClientAsync();
            var subscriber = await GetClientAsync();
            var subscriberId = subscriber.Id;

            var subscriberDone = new ManualResetEventSlim();
            var subscriberReceived = 0;

            await subscriber
                .SubscribeAsync( topic, MqttQualityOfService.AtMostOnce )
                .ConfigureAwait( continueOnCapturedContext: false );

            subscriber
                .MessageStream
                .Where( m => m.Topic == topic )
                .Subscribe( m =>
                {
                    Interlocked.Increment( ref subscriberReceived );

                    if( subscriberReceived == count )
                        subscriberDone.Set();
                } );

            await subscriber.DisconnectAsync();
            var sessionState = await subscriber.ConnectAsync( new MqttClientCredentials( subscriberId ), cleanSession: true );

            var tasks = new List<Task>();

            for( var i = 1; i <= count; i++ )
            {
                var testMessage = GetTestMessage( i );
                var message = new MqttApplicationMessage( topic, Serializer.Serialize( testMessage ) );

                tasks.Add( publisher.PublishAsync( message, MqttQualityOfService.AtMostOnce ) );
            }

            await Task.WhenAll( tasks );

            var completed = subscriberDone.Wait( TimeSpan.FromSeconds( Configuration.WaitTimeoutSecs ) );

            Assert.False( completed );
            SessionState.CleanSession.Should().Be( sessionState );
            0.Should().Be( subscriberReceived );

            await subscriber.UnsubscribeAsync( topic )
                .ConfigureAwait( continueOnCapturedContext: false );

            subscriber.Dispose();
            publisher.Dispose();
        }

        [Test]
        public async Task when_publish_messages_and_client_disconnects_then_message_stream_is_reset()
        {
            var topic = Guid.NewGuid().ToString();

            var publisher = await GetClientAsync();
            var subscriber = await GetClientAsync();
            var subscriberId = subscriber.Id;

            var goal = default( int );
            var goalAchieved = new ManualResetEventSlim();
            var received = 0;

            await subscriber.SubscribeAsync( topic, MqttQualityOfService.AtMostOnce ).ConfigureAwait( continueOnCapturedContext: false );

            subscriber
                .MessageStream
                .Subscribe( m =>
                {
                    if( m.Topic == topic )
                    {
                        received++;

                        if( received == goal )
                            goalAchieved.Set();
                    }
                } );

            goal = 5;

            var tasks = new List<Task>();

            for( var i = 1; i <= goal; i++ )
            {
                var testMessage = GetTestMessage( i );
                var message = new MqttApplicationMessage( topic, Serializer.Serialize( testMessage ) );

                tasks.Add( publisher.PublishAsync( message, MqttQualityOfService.AtMostOnce ) );
            }

            await Task.WhenAll( tasks );

            var completed = goalAchieved.Wait( TimeSpan.FromSeconds( Configuration.WaitTimeoutSecs ) );

            Assert.True( completed );
            goal.Should().Be( received );

            await subscriber.DisconnectAsync();

            goal = 3;
            goalAchieved.Reset();
            received = 0;
            completed = false;

            await subscriber.ConnectAsync( new MqttClientCredentials( subscriberId ), cleanSession: false );

            for( var i = 1; i <= goal; i++ )
            {
                var testMessage = GetTestMessage( i );
                var message = new MqttApplicationMessage( topic, Serializer.Serialize( testMessage ) );

                tasks.Add( publisher.PublishAsync( message, MqttQualityOfService.AtMostOnce ) );
            }

            completed = goalAchieved.Wait( TimeSpan.FromSeconds( Configuration.WaitTimeoutSecs ) );

            Assert.False( completed );
            0.Should().Be( received );

            await subscriber.UnsubscribeAsync( topic ).ConfigureAwait( continueOnCapturedContext: false );

            subscriber.Dispose();
            publisher.Dispose();
        }

        public void Dispose()
        {
            if( server != null )
            {
                server.Stop();
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
