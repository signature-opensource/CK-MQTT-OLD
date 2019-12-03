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
	public class EmptyPacketFormatterSpec
	{
		readonly Mock<IMqttChannel<IPacket>> packetChannel;
		readonly Mock<IMqttChannel<byte[]>> byteChannel;

		public EmptyPacketFormatterSpec ()
		{
			packetChannel = new Mock<IMqttChannel<IPacket>> ();
			byteChannel = new Mock<IMqttChannel<byte[]>> ();
		}
		
		[Theory]
		[TestCase("Files/Binaries/PingResponse.packet", MqttPacketType.PingResponse, typeof(PingResponse))]
		[TestCase("Files/Binaries/PingRequest.packet", MqttPacketType.PingRequest, typeof(PingRequest))]
		[TestCase("Files/Binaries/Disconnect.packet", MqttPacketType.Disconnect, typeof(Disconnect))]
		public async Task when_reading_empty_packet_then_succeeds(string packetPath, MqttPacketType packetType, Type type)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var formatter = GetFormatter (packetType, type);
			var packet = Packet.ReadAllBytes (packetPath);

			var result = await formatter.FormatAsync (packet)
				.ConfigureAwait(continueOnCapturedContext: false);

			Assert.NotNull (result);
		}

		[Theory]
		[TestCase("Files/Binaries/PingResponse_Invalid_HeaderFlag.packet", MqttPacketType.PingResponse, typeof(PingResponse))]
		[TestCase("Files/Binaries/PingRequest_Invalid_HeaderFlag.packet", MqttPacketType.PingRequest, typeof(PingRequest))]
		[TestCase("Files/Binaries/Disconnect_Invalid_HeaderFlag.packet", MqttPacketType.Disconnect, typeof(Disconnect))]
		public void when_reading_invalid_empty_packet_then_fails(string packetPath, MqttPacketType packetType, Type type)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var formatter = GetFormatter (packetType, type);
			var packet = Packet.ReadAllBytes (packetPath);
			
			var ex = Assert.Throws<AggregateException> (() => formatter.FormatAsync (packet).Wait());

			Assert.True (ex.InnerException is MqttException);
		}

		[Theory]
		[TestCase("Files/Binaries/PingResponse.packet", MqttPacketType.PingResponse, typeof(PingResponse))]
		[TestCase("Files/Binaries/PingRequest.packet", MqttPacketType.PingRequest, typeof(PingRequest))]
		[TestCase("Files/Binaries/Disconnect.packet", MqttPacketType.Disconnect, typeof(Disconnect))]
		public async Task when_writing_empty_packet_then_succeeds(string packetPath, MqttPacketType packetType, Type type)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var expectedPacket = Packet.ReadAllBytes (packetPath);
			var formatter = GetFormatter (packetType, type);
			var packet = Activator.CreateInstance (type) as IPacket;

			var result = await formatter.FormatAsync (packet)
				.ConfigureAwait(continueOnCapturedContext: false);

            expectedPacket.Should().BeEquivalentTo( result );
		}

		IFormatter GetFormatter(MqttPacketType packetType, Type type)
		{
			var genericType = typeof (EmptyPacketFormatter<>);
			var formatterType = genericType.MakeGenericType (type);
			var formatter = Activator.CreateInstance (formatterType, packetType) as IFormatter;

			return formatter;
		}
	}
}
