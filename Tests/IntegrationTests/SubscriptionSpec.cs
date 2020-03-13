using CK.Core;
using CK.MQTT;
using FluentAssertions;
using IntegrationTests.Context;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using static CK.Testing.MonitorTestHelper;

namespace IntegrationTests
{
    public abstract class SubscriptionSpec : ConnectedContext
    {
        [Test]
        public async Task when_server_is_closed_then_error_occurs_when_client_send_message()
        {
            try
            {
                (IMqttClient client, IActivityMonitor m) = await GetClientAsync();
                using( client )
                {
                    Server.Stop();

                    AggregateException aggregateException = Assert.Throws<AggregateException>( () => client.SubscribeAsync( TestHelper.Monitor, "test\foo", MqttQualityOfService.AtLeastOnce ).Wait() );

                    Assert.NotNull( aggregateException );
                    Assert.NotNull( aggregateException.InnerException );
                    Assert.True( aggregateException.InnerException is MqttClientException || aggregateException.InnerException is ObjectDisposedException );
                }
            }
            finally
            {
                Setup();
            }

        }

        [Test]
        public async Task when_subscribe_topic_then_succeeds()
        {
            (IMqttClient client, IActivityMonitor m) = await GetClientAsync();
            using( client )
            {
                string topicFilter = Guid.NewGuid().ToString() + "/#";

                await client.SubscribeAsync( m, topicFilter, MqttQualityOfService.AtMostOnce );

                Assert.True( client.CheckConnection( m ) );

                await client.UnsubscribeAsync( m, topicFilter );
            }
        }

        [TestCase( 100 )]
        [TestCase( 200 )]
        public async Task when_subscribe_multiple_topics_then_succeeds( int topicsToSubscribe )
        {
            (IMqttClient client, IActivityMonitor m) = await GetClientAsync();
            using( client )
            {
                List<string> topics = new List<string>();
                List<Task> tasks = new List<Task>();

                for( int i = 1; i <= topicsToSubscribe; i++ )
                {
                    string topicFilter = Guid.NewGuid().ToString();

                    tasks.Add( client.SubscribeAsync( m, topicFilter, MqttQualityOfService.AtMostOnce ) );
                    topics.Add( topicFilter );
                }

                await Task.WhenAll( tasks );

                Assert.True( client.CheckConnection( m ) );

                await client.UnsubscribeAsync( m, topics.ToArray() );
            }
        }

        [Theory]
        [TestCase( "foo/#/#" )]
        [TestCase( "foo/bar#/" )]
        [TestCase( "foo/bar+/test" )]
        [TestCase( "foo/#/bar" )]
        public async Task when_subscribing_invalid_topic_then_fails( string topicFilter )
        {
            (IMqttClient client, IActivityMonitor m) = await GetClientAsync();
            using( client )
            {
                AggregateException ex = Assert.Throws<AggregateException>( () => client.SubscribeAsync( m, topicFilter, MqttQualityOfService.AtMostOnce ).Wait() );

                Assert.NotNull( ex );
                Assert.NotNull( ex.InnerException );
                Assert.True( ex.InnerException is MqttClientException );
                Assert.NotNull( ex.InnerException.InnerException );
                Assert.NotNull( ex.InnerException.InnerException is MqttException );
                ex.InnerException.InnerException.Message.Should().Be( ClientProperties.SubscribeFormatter_InvalidTopicFilter( topicFilter ) );
            }
        }

        [Test]
        public async Task when_subscribing_emtpy_topic_then_fails_with_protocol_violation()
        {
            (IMqttClient client, IActivityMonitor m) = await GetClientAsync();
            using( client )
            {
                AggregateException ex = Assert.Throws<AggregateException>( () => client.SubscribeAsync( m, string.Empty, MqttQualityOfService.AtMostOnce ).Wait() );

                Assert.NotNull( ex );
                Assert.NotNull( ex.InnerException );
                Assert.True( ex.InnerException is MqttClientException );
                Assert.NotNull( ex.InnerException.InnerException );
                Assert.NotNull( ex.InnerException.InnerException is MqttProtocolViolationException );
            }
        }

        [TestCase( 100 )]
        [TestCase( 200 )]
        [TestCase( 500 )]
        public async Task when_unsubscribe_topic_then_succeeds( int topicsToSubscribe )
        {
            (IMqttClient client, IActivityMonitor m) = await GetClientAsync();
            using( client )
            {
                List<string> topics = new List<string>();
                List<Task> tasks = new List<Task>();

                for( int i = 1; i <= topicsToSubscribe; i++ )
                {
                    string topicFilter = Guid.NewGuid().ToString();

                    tasks.Add( client.SubscribeAsync( m, topicFilter, MqttQualityOfService.AtMostOnce ) );
                    topics.Add( topicFilter );
                }

                await Task.WhenAll( tasks );
                await client.UnsubscribeAsync( m, topics.ToArray() );

                Assert.True( client.CheckConnection( m ) );
            }
        }

        [Test]
        public async Task when_unsubscribing_emtpy_topic_then_fails_with_protocol_violation()
        {
            (IMqttClient client, IActivityMonitor m) = await GetClientAsync();
            using( client )
            {
                AggregateException ex = Assert.Throws<AggregateException>( () => client.UnsubscribeAsync( m, null ).Wait() );

                Assert.NotNull( ex );
                Assert.NotNull( ex.InnerException );
                Assert.True( ex.InnerException is MqttClientException );
                Assert.NotNull( ex.InnerException.InnerException );
                Assert.NotNull( ex.InnerException.InnerException is MqttProtocolViolationException );
            }

        }
    }
}
