using System;
using System.IO;
using System.Threading.Tasks;
using CK.MQTT;
using CK.MQTT.Sdk.Formatters;
using CK.MQTT.Sdk.Packets;
using Moq;
using CK.MQTT.Sdk;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.Formatters
{
	public class SubscribeFormatterSpec
	{
		[Theory]
		[TestCase("Files/Binaries/Subscribe_SingleTopic.packet", "Files/Packets/Subscribe_SingleTopic.json")]
		[TestCase("Files/Binaries/Subscribe_MultiTopic.packet", "Files/Packets/Subscribe_MultiTopic.json")]
		public async Task when_reading_subscribe_packet_then_succeeds(string packetPath, string jsonPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);

			var expectedSubscribe = Packet.ReadPacket<Subscribe> (jsonPath);
			var topicEvaluator = Mock.Of<IMqttTopicEvaluator> (e => e.IsValidTopicFilter(It.IsAny<string>()) == true);
			var formatter = new SubscribeFormatter (topicEvaluator);
			var packet = Packet.ReadAllBytes (packetPath);

			var result = await formatter.FormatAsync (packet)
				.ConfigureAwait(continueOnCapturedContext: false);

			expectedSubscribe.Should().Be(result);
		}

		[Theory]
		[TestCase("Files/Binaries/Subscribe_Invalid_HeaderFlag.packet")]
		public void when_reading_invalid_subscribe_packet_then_fails(string packetPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var topicEvaluator = Mock.Of<IMqttTopicEvaluator> (e => e.IsValidTopicFilter(It.IsAny<string>()) == true);
			var formatter = new SubscribeFormatter (topicEvaluator);
			var packet = Packet.ReadAllBytes (packetPath);
			
			var ex = Assert.Throws<AggregateException> (() => formatter.FormatAsync (packet).Wait());

			Assert.True (ex.InnerException is MqttException);
		}

		[Theory]
		[TestCase("Files/Binaries/Subscribe_Invalid_TopicFilterQosPair.packet")]
		[TestCase("Files/Binaries/Subscribe_Invalid_TopicFilterQosPair2.packet")]
		[TestCase("Files/Binaries/Subscribe_Invalid_TopicFilterQos.packet")]
		public void when_reading_invalid_topic_filter_in_subscribe_packet_then_fails(string packetPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var topicEvaluator = Mock.Of<IMqttTopicEvaluator> (e => e.IsValidTopicFilter(It.IsAny<string>()) == true);
			var formatter = new SubscribeFormatter (topicEvaluator);
			var packet = Packet.ReadAllBytes (packetPath);
			
			var ex = Assert.Throws<AggregateException> (() => formatter.FormatAsync (packet).Wait());

			Assert.True (ex.InnerException is MqttProtocolViolationException);
		}

		[Theory]
		[TestCase("Files/Packets/Subscribe_SingleTopic.json", "Files/Binaries/Subscribe_SingleTopic.packet")]
		[TestCase("Files/Packets/Subscribe_MultiTopic.json", "Files/Binaries/Subscribe_MultiTopic.packet")]
		public async Task when_writing_subscribe_packet_then_succeeds(string jsonPath, string packetPath)
		{
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var expectedPacket = Packet.ReadAllBytes (packetPath);
			var topicEvaluator = Mock.Of<IMqttTopicEvaluator> (e => e.IsValidTopicFilter(It.IsAny<string>()) == true);
			var formatter = new SubscribeFormatter (topicEvaluator);
			var subscribe = Packet.ReadPacket<Subscribe> (jsonPath);

			var result = await formatter.FormatAsync (subscribe)
				.ConfigureAwait(continueOnCapturedContext: false);

			expectedPacket.Should().BeEquivalentTo( result);
		}

		[Theory]
		[TestCase("Files/Packets/Subscribe_Invalid_EmptyTopicFilters.json")]
		public void when_writing_invalid_subscribe_packet_then_fails(string jsonPath)
		{
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);

			var topicEvaluator = Mock.Of<IMqttTopicEvaluator> (e => e.IsValidTopicFilter(It.IsAny<string>()) == true);
			var formatter = new SubscribeFormatter (topicEvaluator);
			var subscribe = Packet.ReadPacket<Subscribe> (jsonPath);

			var ex = Assert.Throws<AggregateException> (() => formatter.FormatAsync (subscribe).Wait());

			Assert.True (ex.InnerException is MqttProtocolViolationException);
		}
	}
}
