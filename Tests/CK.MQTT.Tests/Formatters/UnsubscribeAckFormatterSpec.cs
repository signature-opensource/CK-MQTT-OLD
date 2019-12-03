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
	public class UnsubscribeAckFormatterSpec
	{
		[Theory]
		[TestCase("Files/Binaries/UnsubscribeAck.packet", "Files/Packets/UnsubscribeAck.json")]
		public async Task when_reading_unsubscribe_ack_packet_then_succeeds(string packetPath, string jsonPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);

			var expectedUnsubscribeAck = Packet.ReadPacket<UnsubscribeAck> (jsonPath);
			var formatter = new FlowPacketFormatter<UnsubscribeAck>(MqttPacketType.UnsubscribeAck, id => new UnsubscribeAck(id));
			var packet = Packet.ReadAllBytes (packetPath);

			var result = await formatter.FormatAsync (packet)
				.ConfigureAwait(continueOnCapturedContext: false);

			expectedUnsubscribeAck.Should().Be(result);
		}

		[Theory]
		[TestCase("Files/Binaries/UnsubscribeAck_Invalid_HeaderFlag.packet")]
		public void when_reading_invalid_unsubscribe_ack_packet_then_fails(string packetPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var formatter = new FlowPacketFormatter<UnsubscribeAck> (MqttPacketType.UnsubscribeAck, id => new UnsubscribeAck(id));
			var packet = Packet.ReadAllBytes (packetPath);
			
			var ex = Assert.Throws<AggregateException> (() => formatter.FormatAsync (packet).Wait());

			Assert.True (ex.InnerException is MqttException);
		}

		[Theory]
		[TestCase("Files/Packets/UnsubscribeAck.json", "Files/Binaries/UnsubscribeAck.packet")]
		public async Task when_writing_unsubscribe_ack_packet_then_succeeds(string jsonPath, string packetPath)
		{
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var expectedPacket = Packet.ReadAllBytes (packetPath);
			var formatter = new FlowPacketFormatter<UnsubscribeAck>(MqttPacketType.UnsubscribeAck, id => new UnsubscribeAck(id));
			var unsubscribeAck = Packet.ReadPacket<UnsubscribeAck> (jsonPath);

			var result = await formatter.FormatAsync (unsubscribeAck)
				.ConfigureAwait(continueOnCapturedContext: false);

			expectedPacket.Should().BeEquivalentTo( result);
		}
	}
}
