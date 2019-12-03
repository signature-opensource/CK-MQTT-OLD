using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CK.MQTT;
using CK.MQTT.Sdk.Formatters;
using CK.MQTT.Sdk.Packets;
using Moq;
using CK.MQTT.Sdk;
using FluentAssertions;
using NUnit.Framework;

namespace Tests
{
	public class PacketManagerSpec
	{
		[Theory]
		[TestCase("Files/Binaries/Connect_Full.packet", "Files/Packets/Connect_Full.json", typeof(Connect), MqttPacketType.Connect)]
		[TestCase("Files/Binaries/Connect_Min.packet", "Files/Packets/Connect_Min.json", typeof(Connect), MqttPacketType.Connect)]
		[TestCase("Files/Binaries/ConnectAck.packet", "Files/Packets/ConnectAck.json", typeof(ConnectAck), MqttPacketType.ConnectAck)]
		[TestCase("Files/Binaries/Publish_Full.packet", "Files/Packets/Publish_Full.json", typeof(Publish), MqttPacketType.Publish)]
		[TestCase("Files/Binaries/Publish_Min.packet", "Files/Packets/Publish_Min.json", typeof(Publish), MqttPacketType.Publish)]
		[TestCase("Files/Binaries/PublishAck.packet", "Files/Packets/PublishAck.json", typeof(PublishAck), MqttPacketType.PublishAck)]
		[TestCase("Files/Binaries/PublishComplete.packet", "Files/Packets/PublishComplete.json", typeof(PublishComplete), MqttPacketType.PublishComplete)]
		[TestCase("Files/Binaries/PublishReceived.packet", "Files/Packets/PublishReceived.json", typeof(PublishReceived), MqttPacketType.PublishReceived)]
		[TestCase("Files/Binaries/PublishRelease.packet", "Files/Packets/PublishRelease.json", typeof(PublishRelease), MqttPacketType.PublishRelease)]
		[TestCase("Files/Binaries/Subscribe_MultiTopic.packet", "Files/Packets/Subscribe_MultiTopic.json", typeof(Subscribe), MqttPacketType.Subscribe)]
		[TestCase("Files/Binaries/Subscribe_SingleTopic.packet", "Files/Packets/Subscribe_SingleTopic.json", typeof(Subscribe), MqttPacketType.Subscribe)]
		[TestCase("Files/Binaries/SubscribeAck_MultiTopic.packet", "Files/Packets/SubscribeAck_MultiTopic.json", typeof(SubscribeAck), MqttPacketType.SubscribeAck)]
		[TestCase("Files/Binaries/SubscribeAck_SingleTopic.packet", "Files/Packets/SubscribeAck_SingleTopic.json", typeof(SubscribeAck), MqttPacketType.SubscribeAck)]
		[TestCase("Files/Binaries/Unsubscribe_MultiTopic.packet", "Files/Packets/Unsubscribe_MultiTopic.json", typeof(Unsubscribe), MqttPacketType.Unsubscribe)]
		[TestCase("Files/Binaries/Unsubscribe_SingleTopic.packet", "Files/Packets/Unsubscribe_SingleTopic.json", typeof(Unsubscribe), MqttPacketType.Unsubscribe)]
		[TestCase("Files/Binaries/UnsubscribeAck.packet", "Files/Packets/UnsubscribeAck.json", typeof(UnsubscribeAck), MqttPacketType.UnsubscribeAck)]
		public async Task when_managing_packet_bytes_then_succeeds(string packetPath, string jsonPath, Type packetType, MqttPacketType type)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);
			
			var bytes = Packet.ReadAllBytes (packetPath);
			
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);
			
			object packet = Packet.ReadPacket(jsonPath, packetType);
			var formatter = new Mock<IFormatter> ();

			formatter.Setup(f => f.PacketType).Returns(type);

			formatter
				.Setup (f => f.FormatAsync (It.Is<byte[]> (b => b.ToList().SequenceEqual(bytes))))
				.Returns (Task.FromResult<IPacket>((IPacket)packet));

			var packetManager = new PacketManager (formatter.Object);
			IPacket result =  await packetManager.GetPacketAsync (bytes)
				.ConfigureAwait(continueOnCapturedContext: false);
            packet.Should().Be(result);
		}

		[TestCase("Files/Binaries/Connect_Full.packet", "Files/Packets/Connect_Full.json", typeof(Connect), MqttPacketType.Connect)]
		[TestCase("Files/Binaries/Connect_Min.packet", "Files/Packets/Connect_Min.json", typeof(Connect), MqttPacketType.Connect)]
		[TestCase("Files/Binaries/ConnectAck.packet", "Files/Packets/ConnectAck.json", typeof(ConnectAck), MqttPacketType.ConnectAck)]
		[TestCase("Files/Binaries/Publish_Full.packet", "Files/Packets/Publish_Full.json", typeof(Publish), MqttPacketType.Publish)]
		[TestCase("Files/Binaries/Publish_Min.packet", "Files/Packets/Publish_Min.json", typeof(Publish), MqttPacketType.Publish)]
		[TestCase("Files/Binaries/PublishAck.packet", "Files/Packets/PublishAck.json", typeof(PublishAck), MqttPacketType.PublishAck)]
		[TestCase("Files/Binaries/PublishComplete.packet", "Files/Packets/PublishComplete.json", typeof(PublishComplete), MqttPacketType.PublishComplete)]
		[TestCase("Files/Binaries/PublishReceived.packet", "Files/Packets/PublishReceived.json", typeof(PublishReceived), MqttPacketType.PublishReceived)]
		[TestCase("Files/Binaries/PublishRelease.packet", "Files/Packets/PublishRelease.json", typeof(PublishRelease), MqttPacketType.PublishRelease)]
		[TestCase("Files/Binaries/Subscribe_MultiTopic.packet", "Files/Packets/Subscribe_MultiTopic.json", typeof(Subscribe), MqttPacketType.Subscribe)]
		[TestCase("Files/Binaries/Subscribe_SingleTopic.packet", "Files/Packets/Subscribe_SingleTopic.json", typeof(Subscribe), MqttPacketType.Subscribe)]
		[TestCase("Files/Binaries/SubscribeAck_MultiTopic.packet", "Files/Packets/SubscribeAck_MultiTopic.json", typeof(SubscribeAck), MqttPacketType.SubscribeAck)]
		[TestCase("Files/Binaries/SubscribeAck_SingleTopic.packet", "Files/Packets/SubscribeAck_SingleTopic.json", typeof(SubscribeAck), MqttPacketType.SubscribeAck)]
		[TestCase("Files/Binaries/Unsubscribe_MultiTopic.packet", "Files/Packets/Unsubscribe_MultiTopic.json", typeof(Unsubscribe), MqttPacketType.Unsubscribe)]
		[TestCase("Files/Binaries/Unsubscribe_SingleTopic.packet", "Files/Packets/Unsubscribe_SingleTopic.json", typeof(Unsubscribe), MqttPacketType.Unsubscribe)]
		[TestCase("Files/Binaries/UnsubscribeAck.packet", "Files/Packets/UnsubscribeAck.json", typeof(UnsubscribeAck), MqttPacketType.UnsubscribeAck)]
		public async Task when_managing_packet_from_source_then_succeeds(string packetPath, string jsonPath, Type packetType, MqttPacketType type)
		{
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);
			
			var packet = Packet.ReadPacket (jsonPath, packetType) as IPacket;
			
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);
			
			var bytes = Packet.ReadAllBytes (packetPath);
			var formatter = new Mock<IFormatter> ();

			formatter.Setup(f => f.PacketType).Returns(type);

			formatter
				.Setup (f => f.FormatAsync (It.Is<IPacket> (p => Convert.ChangeType(p, packetType) == packet)))
				.Returns (Task.FromResult(bytes));

			var packetManager = new PacketManager (formatter.Object);
			var result = await packetManager.GetBytesAsync (packet)
				.ConfigureAwait(continueOnCapturedContext: false);
            bytes.Should().BeEquivalentTo( result );
		}

		[Theory]
		[TestCase("Files/Binaries/Disconnect.packet", typeof(Disconnect), MqttPacketType.Disconnect)]
		[TestCase("Files/Binaries/PingRequest.packet", typeof(PingRequest), MqttPacketType.PingRequest)]
		[TestCase("Files/Binaries/PingResponse.packet", typeof(PingResponse), MqttPacketType.PingResponse)]
		public async Task when_managing_packet_then_succeeds(string packetPath, Type packetType, MqttPacketType type)
		{
			var packet = Activator.CreateInstance (packetType) as IPacket;
			
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);
			
			var bytes = Packet.ReadAllBytes (packetPath);
			var formatter = new Mock<IFormatter> ();

			formatter.Setup(f => f.PacketType).Returns(type);

			formatter
				.Setup (f => f.FormatAsync (It.Is<IPacket> (p => Convert.ChangeType(p, packetType) == packet)))
				.Returns (Task.FromResult(bytes));

			var packetManager = new PacketManager (formatter.Object);
			var result = await packetManager.GetBytesAsync (packet)
				.ConfigureAwait(continueOnCapturedContext: false);

            bytes.Should().BeEquivalentTo( result );
		}

		[Theory]
		[TestCase("Files/Binaries/Connect_Full.packet")]
		[TestCase("Files/Binaries/Connect_Min.packet")]
		[TestCase("Files/Binaries/ConnectAck.packet")]
		[TestCase("Files/Binaries/Disconnect.packet")]
		[TestCase("Files/Binaries/PingRequest.packet")]
		[TestCase("Files/Binaries/PingResponse.packet")]
		[TestCase("Files/Binaries/Publish_Full.packet")]
		[TestCase("Files/Binaries/Publish_Min.packet")]
		[TestCase("Files/Binaries/PublishAck.packet")]
		[TestCase("Files/Binaries/PublishComplete.packet")]
		[TestCase("Files/Binaries/PublishReceived.packet")]
		[TestCase("Files/Binaries/PublishRelease.packet")]
		[TestCase("Files/Binaries/Subscribe_MultiTopic.packet")]
		[TestCase("Files/Binaries/Subscribe_SingleTopic.packet")]
		[TestCase("Files/Binaries/SubscribeAck_MultiTopic.packet")]
		[TestCase("Files/Binaries/SubscribeAck_SingleTopic.packet")]
		[TestCase("Files/Binaries/Unsubscribe_MultiTopic.packet")]
		[TestCase("Files/Binaries/Unsubscribe_SingleTopic.packet")]
		[TestCase("Files/Binaries/UnsubscribeAck.packet")]
		public void when_managing_unknown_packet_bytes_then_fails(string packetPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var packet = Packet.ReadAllBytes (packetPath);
			var formatter = new Mock<IFormatter> ();
			var packetManager = new PacketManager (formatter.Object);

			var ex = Assert.Throws<AggregateException> (() => packetManager.GetPacketAsync (packet).Wait());

			ex.InnerException.Should().BeOfType<MqttException>();
		}

		[Theory]
		[TestCase("Files/Packets/Connect_Full.json")]
		[TestCase("Files/Packets/Connect_Min.json")]
		[TestCase("Files/Packets/ConnectAck.json")]
		[TestCase("Files/Packets/Publish_Full.json")]
		[TestCase("Files/Packets/Publish_Min.json")]
		[TestCase("Files/Packets/PublishAck.json")]
		[TestCase("Files/Packets/PublishComplete.json")]
		[TestCase("Files/Packets/PublishReceived.json")]
		[TestCase("Files/Packets/PublishRelease.json")]
		[TestCase("Files/Packets/Subscribe_MultiTopic.json")]
		[TestCase("Files/Packets/Subscribe_SingleTopic.json")]
		[TestCase("Files/Packets/SubscribeAck_MultiTopic.json")]
		[TestCase("Files/Packets/SubscribeAck_SingleTopic.json")]
		[TestCase("Files/Packets/Unsubscribe_MultiTopic.json")]
		[TestCase("Files/Packets/Unsubscribe_SingleTopic.json")]
		[TestCase("Files/Packets/UnsubscribeAck.json")]
		public void when_managing_unknown_packet_then_fails(string jsonPath)
		{
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);
			
			var packet = Packet.ReadPacket<Connect> (jsonPath);
			var formatter = new Mock<IFormatter> ();
			var packetManager = new PacketManager (formatter.Object);

			var ex = Assert.Throws<AggregateException> (() => packetManager.GetBytesAsync (packet).Wait());

			Assert.True (ex.InnerException is MqttException);
		}
	}
}
