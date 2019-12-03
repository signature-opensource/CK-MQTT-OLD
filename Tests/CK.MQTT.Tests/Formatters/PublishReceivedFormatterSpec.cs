using System;
using System.IO;
using System.Threading.Tasks;
using CK.MQTT.Sdk.Formatters;
using CK.MQTT.Sdk.Packets;
using CK.MQTT;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.Formatters
{
	public class PublishReceivedFormatterSpec
	{
		[Theory]
		[TestCase("Files/Binaries/PublishReceived.packet", "Files/Packets/PublishReceived.json")]
		public async Task when_reading_publish_received_packet_then_succeeds(string packetPath, string jsonPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);

			var expectedPublishReceived = Packet.ReadPacket<PublishReceived> (jsonPath);
			var formatter = new FlowPacketFormatter<PublishReceived>(MqttPacketType.PublishReceived, id => new PublishReceived(id));
			var packet = Packet.ReadAllBytes (packetPath);

			var result = await formatter.FormatAsync (packet)
				.ConfigureAwait(continueOnCapturedContext: false);

			expectedPublishReceived.Should().Be(result);
		}

		[Theory]
		[TestCase("Files/Binaries/PublishReceived_Invalid_HeaderFlag.packet")]
		public void when_reading_invalid_publish_received_packet_then_fails(string packetPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var formatter = new FlowPacketFormatter<PublishReceived> (MqttPacketType.PublishReceived, id => new PublishReceived(id));
			var packet = Packet.ReadAllBytes (packetPath);
			
			var ex = Assert.Throws<AggregateException> (() => formatter.FormatAsync (packet).Wait());

			Assert.True (ex.InnerException is MqttException);
		}

		[Theory]
		[TestCase("Files/Packets/PublishReceived.json", "Files/Binaries/PublishReceived.packet")]
		public async Task when_writing_publish_received_packet_then_succeeds(string jsonPath, string packetPath)
		{
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var expectedPacket = Packet.ReadAllBytes (packetPath);
			var formatter = new FlowPacketFormatter<PublishReceived>(MqttPacketType.PublishReceived, id => new PublishReceived(id));
			var publishReceived = Packet.ReadPacket<PublishReceived> (jsonPath);

			var result = await formatter.FormatAsync (publishReceived)
				.ConfigureAwait(continueOnCapturedContext: false);

			expectedPacket.Should().BeEquivalentTo( result);
		}
	}
}
