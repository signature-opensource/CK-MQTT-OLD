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
	public class PublishFormatterSpec
	{
		readonly Mock<IMqttChannel<IPacket>> packetChannel;
		readonly Mock<IMqttChannel<byte[]>> byteChannel;

		public PublishFormatterSpec ()
		{
			packetChannel = new Mock<IMqttChannel<IPacket>> ();
			byteChannel = new Mock<IMqttChannel<byte[]>> ();
		}
		
		[Theory]
		[TestCase("Files/Binaries/Publish_Full.packet", "Files/Packets/Publish_Full.json")]
		[TestCase("Files/Binaries/Publish_Min.packet", "Files/Packets/Publish_Min.json")]
		public async Task when_reading_publish_packet_then_succeeds(string packetPath, string jsonPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);

			var expectedPublish = Packet.ReadPacket<Publish> (jsonPath);
			var topicEvaluator = Mock.Of<IMqttTopicEvaluator> (e => e.IsValidTopicName(It.IsAny<string>()) == true);
			var formatter = new PublishFormatter (topicEvaluator);
			var packet = Packet.ReadAllBytes (packetPath);

			var result = await formatter.FormatAsync (packet)
				.ConfigureAwait(continueOnCapturedContext: false);

			expectedPublish.Should().Be(result);
		}

		[Theory]
		[TestCase("Files/Binaries/Publish_Invalid_QualityOfService.packet")]
		[TestCase("Files/Binaries/Publish_Invalid_Duplicated.packet")]
		[TestCase("Files/Binaries/Publish_Invalid_Topic.packet")]
		public void when_reading_invalid_publish_packet_then_fails(string packetPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var topicEvaluator = new MqttTopicEvaluator (new MqttConfiguration());
			var formatter = new PublishFormatter (topicEvaluator);
			var packet = Packet.ReadAllBytes (packetPath);
			
			var ex = Assert.Throws<AggregateException> (() => formatter.FormatAsync (packet).Wait());

			Assert.True (ex.InnerException is MqttException);
		}

		[Theory]
		[TestCase("Files/Packets/Publish_Full.json", "Files/Binaries/Publish_Full.packet")]
		[TestCase("Files/Packets/Publish_Min.json", "Files/Binaries/Publish_Min.packet")]
		public async Task when_writing_publish_packet_then_succeeds(string jsonPath, string packetPath)
		{
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var expectedPacket = Packet.ReadAllBytes (packetPath);
			var topicEvaluator = Mock.Of<IMqttTopicEvaluator> (e => e.IsValidTopicName(It.IsAny<string>()) == true);
			var formatter = new PublishFormatter (topicEvaluator);
			var publish = Packet.ReadPacket<Publish> (jsonPath);

			var result = await formatter.FormatAsync (publish)
				.ConfigureAwait(continueOnCapturedContext: false);

			expectedPacket.Should().BeEquivalentTo( result);
		}

		[Theory]
		[TestCase("Files/Packets/Publish_Invalid_Duplicated.json")]
		[TestCase("Files/Packets/Publish_Invalid_Topic.json")]
		[TestCase("Files/Packets/Publish_Invalid_PacketId.json")]
		public void when_writing_invalid_publish_packet_then_fails(string jsonPath)
		{
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);

			var topicEvaluator = new MqttTopicEvaluator (new MqttConfiguration());
			var formatter = new PublishFormatter (topicEvaluator);
			var publish = Packet.ReadPacket<Publish> (jsonPath);

			var ex = Assert.Throws<AggregateException> (() => formatter.FormatAsync (publish).Wait());

			Assert.True (ex.InnerException is MqttException);
		}
	}
}
