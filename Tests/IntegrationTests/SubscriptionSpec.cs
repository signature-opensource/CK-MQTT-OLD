using IntegrationTests.Context;
using System;
using System.Collections.Generic;
using CK.MQTT;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace IntegrationTests
{
	public class SubscriptionSpec : ConnectedContext
	{
		readonly IMqttServer server;

		public SubscriptionSpec ()
		{
			server = GetServerAsync ().Result;
		}

        [Test]
        public async Task when_server_is_closed_then_error_occurs_when_client_send_message()
        {
            var client = await GetClientAsync();

            server.Stop();

            var aggregateException = Assert.Throws<AggregateException>(() => client.SubscribeAsync("test\foo", MqttQualityOfService.AtLeastOnce).Wait());

            Assert.NotNull(aggregateException);
            Assert.NotNull(aggregateException.InnerException);
            Assert.True(aggregateException.InnerException is MqttClientException || aggregateException.InnerException is ObjectDisposedException);
        }

        [Test]
		public async Task when_subscribe_topic_then_succeeds()
		{
			var client = await GetClientAsync ();
			var topicFilter = Guid.NewGuid ().ToString () + "/#";

			await client.SubscribeAsync (topicFilter, MqttQualityOfService.AtMostOnce)
				.ConfigureAwait(continueOnCapturedContext: false);

			Assert.True (client.IsConnected);

			await client.UnsubscribeAsync (topicFilter);

			client.Dispose ();
		}

		[Test]
		public async Task when_subscribe_multiple_topics_then_succeeds()
		{
			var client = await GetClientAsync ();
			var topicsToSubscribe = GetTestLoad();
			var topics = new List<string> ();
			var tasks = new List<Task> ();

			for (var i = 1; i <= topicsToSubscribe; i++) {
				var topicFilter = Guid.NewGuid ().ToString ();

				tasks.Add (client.SubscribeAsync (topicFilter, MqttQualityOfService.AtMostOnce));
				topics.Add (topicFilter);
			}

			await Task.WhenAll (tasks);

			Assert.True (client.IsConnected);

			await client.UnsubscribeAsync (topics.ToArray ());

			client.Dispose ();
		}

        [Theory]
        [TestCase("foo/#/#")]
        [TestCase("foo/bar#/")]
        [TestCase("foo/bar+/test")]
        [TestCase("foo/#/bar")]
        public async Task when_subscribing_invalid_topic_then_fails(string topicFilter)
        {
            var client = await GetClientAsync ();

            var ex = Assert.Throws<AggregateException> (() => client.SubscribeAsync (topicFilter, MqttQualityOfService.AtMostOnce).Wait ());

            Assert.NotNull (ex);
            Assert.NotNull (ex.InnerException);
            Assert.True (ex.InnerException is MqttClientException);
            Assert.NotNull (ex.InnerException.InnerException);
            Assert.NotNull (ex.InnerException.InnerException is MqttException);
            string.Format (Properties.Resources.GetString("SubscribeFormatter_InvalidTopicFilter"), topicFilter).Should().Be(ex.InnerException.InnerException.Message);
        }

        [Test]
        public async Task when_subscribing_emtpy_topic_then_fails_with_protocol_violation()
        {
            var client = await GetClientAsync();

            var ex = Assert.Throws<AggregateException> (() => client.SubscribeAsync (string.Empty, MqttQualityOfService.AtMostOnce).Wait ());

            Assert.NotNull (ex);
            Assert.NotNull (ex.InnerException);
            Assert.True (ex.InnerException is MqttClientException);
            Assert.NotNull (ex.InnerException.InnerException);
            Assert.NotNull (ex.InnerException.InnerException is MqttProtocolViolationException);
        }

        [Test]
		public async Task when_unsubscribe_topic_then_succeeds()
		{
			var client = await GetClientAsync ();
			var topicsToSubscribe = GetTestLoad ();
			var topics = new List<string> ();
			var tasks = new List<Task> ();

			for (var i = 1; i <= topicsToSubscribe; i++) {
				var topicFilter = Guid.NewGuid ().ToString ();

				tasks.Add (client.SubscribeAsync (topicFilter, MqttQualityOfService.AtMostOnce));
				topics.Add (topicFilter);
			}

			await Task.WhenAll (tasks);
			await client.UnsubscribeAsync (topics.ToArray())
				.ConfigureAwait(continueOnCapturedContext: false);

			Assert.True (client.IsConnected);

			client.Dispose ();
		}

        [Test]
        public async Task when_unsubscribing_emtpy_topic_then_fails_with_protocol_violation()
        {
            var client = await GetClientAsync ();

            var ex = Assert.Throws<AggregateException> (() => client.UnsubscribeAsync (null).Wait ());

            Assert.NotNull (ex);
            Assert.NotNull (ex.InnerException);
            Assert.True (ex.InnerException is MqttClientException);
            Assert.NotNull (ex.InnerException.InnerException);
            Assert.NotNull (ex.InnerException.InnerException is MqttProtocolViolationException);

        }
        public void Dispose ()
		{
			if (server != null) {
				server.Stop ();
			}
		}
	}
}
