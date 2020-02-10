using CK.MQTT;
using FluentAssertions;
using IntegrationTests.Context;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IntegrationTests
{
    public abstract class SubscriptionSpec : ConnectedContext
    {
        [Test]
        public async Task when_server_is_closed_then_error_occurs_when_client_send_message()
        {
            IMqttClient client = await GetClientAsync();

            Server.Stop();

            AggregateException aggregateException = Assert.Throws<AggregateException>( () => client.SubscribeAsync( "test\foo", MqttQualityOfService.AtLeastOnce ).Wait() );

            Assert.NotNull( aggregateException );
            Assert.NotNull( aggregateException.InnerException );
            Assert.True( aggregateException.InnerException is MqttClientException || aggregateException.InnerException is ObjectDisposedException );
        }

        [Test]
        public async Task when_subscribe_topic_then_succeeds()
        {
            IMqttClient client = await GetClientAsync();
            string topicFilter = Guid.NewGuid().ToString() + "/#";

            await client.SubscribeAsync( topicFilter, MqttQualityOfService.AtMostOnce )
                .ConfigureAwait( continueOnCapturedContext: false );

            Assert.True( client.IsConnected );

            await client.UnsubscribeAsync( topicFilter );

            client.Dispose();
        }

        [TestCase( 100 )]
        [TestCase( 200 )]
        public async Task when_subscribe_multiple_topics_then_succeeds( int topicsToSubscribe )
        {
            IMqttClient client = await GetClientAsync();
            List<string> topics = new List<string>();
            List<Task> tasks = new List<Task>();

            for( int i = 1; i <= topicsToSubscribe; i++ )
            {
                string topicFilter = Guid.NewGuid().ToString();

                tasks.Add( client.SubscribeAsync( topicFilter, MqttQualityOfService.AtMostOnce ) );
                topics.Add( topicFilter );
            }

            await Task.WhenAll( tasks );

            Assert.True( client.IsConnected );

            await client.UnsubscribeAsync( topics.ToArray() );

            client.Dispose();
        }

        [Theory]
        [TestCase( "foo/#/#" )]
        [TestCase( "foo/bar#/" )]
        [TestCase( "foo/bar+/test" )]
        [TestCase( "foo/#/bar" )]
        public async Task when_subscribing_invalid_topic_then_fails( string topicFilter )
        {
            IMqttClient client = await GetClientAsync();

            AggregateException ex = Assert.Throws<AggregateException>( () => client.SubscribeAsync( topicFilter, MqttQualityOfService.AtMostOnce ).Wait() );

            Assert.NotNull( ex );
            Assert.NotNull( ex.InnerException );
            Assert.True( ex.InnerException is MqttClientException );
            Assert.NotNull( ex.InnerException.InnerException );
            Assert.NotNull( ex.InnerException.InnerException is MqttException );
            string.Format( Properties.Resources.GetString( "SubscribeFormatter_InvalidTopicFilter" ), topicFilter ).Should().Be( ex.InnerException.InnerException.Message );
        }

        [Test]
        public async Task when_subscribing_emtpy_topic_then_fails_with_protocol_violation()
        {
            IMqttClient client = await GetClientAsync();

            AggregateException ex = Assert.Throws<AggregateException>( () => client.SubscribeAsync( string.Empty, MqttQualityOfService.AtMostOnce ).Wait() );

            Assert.NotNull( ex );
            Assert.NotNull( ex.InnerException );
            Assert.True( ex.InnerException is MqttClientException );
            Assert.NotNull( ex.InnerException.InnerException );
            Assert.NotNull( ex.InnerException.InnerException is MqttProtocolViolationException );
        }

        [TestCase( 100 )]
        [TestCase( 200 )]
        [TestCase( 500 )]
        public async Task when_unsubscribe_topic_then_succeeds( int topicsToSubscribe )
        {
            IMqttClient client = await GetClientAsync();
            List<string> topics = new List<string>();
            List<Task> tasks = new List<Task>();

            for( int i = 1; i <= topicsToSubscribe; i++ )
            {
                string topicFilter = Guid.NewGuid().ToString();

                tasks.Add( client.SubscribeAsync( topicFilter, MqttQualityOfService.AtMostOnce ) );
                topics.Add( topicFilter );
            }

            await Task.WhenAll( tasks );
            await client.UnsubscribeAsync( topics.ToArray() )
                .ConfigureAwait( continueOnCapturedContext: false );

            Assert.True( client.IsConnected );

            client.Dispose();
        }

        [Test]
        public async Task when_unsubscribing_emtpy_topic_then_fails_with_protocol_violation()
        {
            IMqttClient client = await GetClientAsync();

            AggregateException ex = Assert.Throws<AggregateException>( () => client.UnsubscribeAsync( null ).Wait() );

            Assert.NotNull( ex );
            Assert.NotNull( ex.InnerException );
            Assert.True( ex.InnerException is MqttClientException );
            Assert.NotNull( ex.InnerException.InnerException );
            Assert.NotNull( ex.InnerException.InnerException is MqttProtocolViolationException );

        }
    }
}
