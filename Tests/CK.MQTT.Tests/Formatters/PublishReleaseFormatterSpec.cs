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
	public class PublishReleaseFormatterSpec
	{
		[Theory]
		[TestCase("Files/Binaries/PublishRelease.packet", "Files/Packets/PublishRelease.json")]
		public async Task when_reading_publish_release_packet_then_succeeds(string packetPath, string jsonPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);

			var expectedPublishRelease = Packet.ReadPacket<PublishRelease> (jsonPath);
			var formatter = new FlowPacketFormatter<PublishRelease>(MqttPacketType.PublishRelease, id => new PublishRelease(id));
			var packet = Packet.ReadAllBytes (packetPath);

			var result = await formatter.FormatAsync (packet)
				.ConfigureAwait(continueOnCapturedContext: false);

			expectedPublishRelease.Should().Be(result);
		}

		[Theory]
		[TestCase("Files/Binaries/PublishRelease_Invalid_HeaderFlag.packet")]
		public void when_reading_invalid_publish_release_packet_then_fails(string packetPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var formatter = new FlowPacketFormatter<PublishRelease> (MqttPacketType.PublishRelease, id => new PublishRelease(id));
			var packet = Packet.ReadAllBytes (packetPath);
			
			var ex = Assert.Throws<AggregateException> (() => formatter.FormatAsync (packet).Wait());

			Assert.True (ex.InnerException is MqttException);
		}

		[Theory]
		[TestCase("Files/Packets/PublishRelease.json", "Files/Binaries/PublishRelease.packet")]
		public async Task when_writing_publish_release_packet_then_succeeds(string jsonPath, string packetPath)
		{
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var expectedPacket = Packet.ReadAllBytes (packetPath);
			var formatter = new FlowPacketFormatter<PublishRelease>(MqttPacketType.PublishRelease, id => new PublishRelease(id));
			var publishRelease = Packet.ReadPacket<PublishRelease> (jsonPath);

			var result = await formatter.FormatAsync (publishRelease)
				.ConfigureAwait(continueOnCapturedContext: false);

			expectedPacket.Should().BeEquivalentTo( result);
		}
	}
}
