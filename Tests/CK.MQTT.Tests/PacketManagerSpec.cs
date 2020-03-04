using CK.Core;
using CK.MQTT;

using CK.MQTT.Sdk;
using CK.MQTT.Sdk.Formatters;
using CK.MQTT.Sdk.Packets;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using static CK.Testing.MonitorTestHelper;

namespace Tests
{
    public class PacketManagerSpec
    {
        [Theory]
        [TestCase( "Files/Binaries/Connect_Full.packet", "Files/Packets/Connect_Full.json", typeof( Connect ), MqttPacketType.Connect )]
        [TestCase( "Files/Binaries/Connect_Min.packet", "Files/Packets/Connect_Min.json", typeof( Connect ), MqttPacketType.Connect )]
        [TestCase( "Files/Binaries/ConnectAck.packet", "Files/Packets/ConnectAck.json", typeof( ConnectAck ), MqttPacketType.ConnectAck )]
        [TestCase( "Files/Binaries/Publish_Full.packet", "Files/Packets/Publish_Full.json", typeof( Publish ), MqttPacketType.Publish )]
        [TestCase( "Files/Binaries/Publish_Min.packet", "Files/Packets/Publish_Min.json", typeof( Publish ), MqttPacketType.Publish )]
        [TestCase( "Files/Binaries/PublishAck.packet", "Files/Packets/PublishAck.json", typeof( PublishAck ), MqttPacketType.PublishAck )]
        [TestCase( "Files/Binaries/PublishComplete.packet", "Files/Packets/PublishComplete.json", typeof( PublishComplete ), MqttPacketType.PublishComplete )]
        [TestCase( "Files/Binaries/PublishReceived.packet", "Files/Packets/PublishReceived.json", typeof( PublishReceived ), MqttPacketType.PublishReceived )]
        [TestCase( "Files/Binaries/PublishRelease.packet", "Files/Packets/PublishRelease.json", typeof( PublishRelease ), MqttPacketType.PublishRelease )]
        [TestCase( "Files/Binaries/Subscribe_MultiTopic.packet", "Files/Packets/Subscribe_MultiTopic.json", typeof( Subscribe ), MqttPacketType.Subscribe )]
        [TestCase( "Files/Binaries/Subscribe_SingleTopic.packet", "Files/Packets/Subscribe_SingleTopic.json", typeof( Subscribe ), MqttPacketType.Subscribe )]
        [TestCase( "Files/Binaries/SubscribeAck_MultiTopic.packet", "Files/Packets/SubscribeAck_MultiTopic.json", typeof( SubscribeAck ), MqttPacketType.SubscribeAck )]
        [TestCase( "Files/Binaries/SubscribeAck_SingleTopic.packet", "Files/Packets/SubscribeAck_SingleTopic.json", typeof( SubscribeAck ), MqttPacketType.SubscribeAck )]
        [TestCase( "Files/Binaries/Unsubscribe_MultiTopic.packet", "Files/Packets/Unsubscribe_MultiTopic.json", typeof( Unsubscribe ), MqttPacketType.Unsubscribe )]
        [TestCase( "Files/Binaries/Unsubscribe_SingleTopic.packet", "Files/Packets/Unsubscribe_SingleTopic.json", typeof( Unsubscribe ), MqttPacketType.Unsubscribe )]
        [TestCase( "Files/Binaries/UnsubscribeAck.packet", "Files/Packets/UnsubscribeAck.json", typeof( UnsubscribeAck ), MqttPacketType.UnsubscribeAck )]
        public async Task when_managing_packet_bytes_then_succeeds( string packetPath, string jsonPath, Type packetType, MqttPacketType type )
        {
            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            byte[] bytes = Packet.ReadAllBytes( packetPath );

            jsonPath = Path.Combine( Environment.CurrentDirectory, jsonPath );

            object packet = Packet.ReadPacket( jsonPath, packetType );
            Mock<IFormatter> formatter = new Mock<IFormatter>();

            formatter.Setup( f => f.PacketType ).Returns( type );

            formatter
                .Setup( f => f.FormatAsync( It.Is<byte[]>( b => b.ToList().SequenceEqual( bytes ) ) ) )
                .Returns( Task.FromResult( (IPacket)packet ) );

            PacketManager packetManager = new PacketManager( formatter.Object );
            var result = await packetManager.GetPacketAsync( Monitored<byte[]>.Create( TestHelper.Monitor, bytes ) );
            packet.Should().Be( result.Item );
        }

        [TestCase( "Files/Binaries/Connect_Full.packet", "Files/Packets/Connect_Full.json", typeof( Connect ), MqttPacketType.Connect )]
        [TestCase( "Files/Binaries/Connect_Min.packet", "Files/Packets/Connect_Min.json", typeof( Connect ), MqttPacketType.Connect )]
        [TestCase( "Files/Binaries/ConnectAck.packet", "Files/Packets/ConnectAck.json", typeof( ConnectAck ), MqttPacketType.ConnectAck )]
        [TestCase( "Files/Binaries/Publish_Full.packet", "Files/Packets/Publish_Full.json", typeof( Publish ), MqttPacketType.Publish )]
        [TestCase( "Files/Binaries/Publish_Min.packet", "Files/Packets/Publish_Min.json", typeof( Publish ), MqttPacketType.Publish )]
        [TestCase( "Files/Binaries/PublishAck.packet", "Files/Packets/PublishAck.json", typeof( PublishAck ), MqttPacketType.PublishAck )]
        [TestCase( "Files/Binaries/PublishComplete.packet", "Files/Packets/PublishComplete.json", typeof( PublishComplete ), MqttPacketType.PublishComplete )]
        [TestCase( "Files/Binaries/PublishReceived.packet", "Files/Packets/PublishReceived.json", typeof( PublishReceived ), MqttPacketType.PublishReceived )]
        [TestCase( "Files/Binaries/PublishRelease.packet", "Files/Packets/PublishRelease.json", typeof( PublishRelease ), MqttPacketType.PublishRelease )]
        [TestCase( "Files/Binaries/Subscribe_MultiTopic.packet", "Files/Packets/Subscribe_MultiTopic.json", typeof( Subscribe ), MqttPacketType.Subscribe )]
        [TestCase( "Files/Binaries/Subscribe_SingleTopic.packet", "Files/Packets/Subscribe_SingleTopic.json", typeof( Subscribe ), MqttPacketType.Subscribe )]
        [TestCase( "Files/Binaries/SubscribeAck_MultiTopic.packet", "Files/Packets/SubscribeAck_MultiTopic.json", typeof( SubscribeAck ), MqttPacketType.SubscribeAck )]
        [TestCase( "Files/Binaries/SubscribeAck_SingleTopic.packet", "Files/Packets/SubscribeAck_SingleTopic.json", typeof( SubscribeAck ), MqttPacketType.SubscribeAck )]
        [TestCase( "Files/Binaries/Unsubscribe_MultiTopic.packet", "Files/Packets/Unsubscribe_MultiTopic.json", typeof( Unsubscribe ), MqttPacketType.Unsubscribe )]
        [TestCase( "Files/Binaries/Unsubscribe_SingleTopic.packet", "Files/Packets/Unsubscribe_SingleTopic.json", typeof( Unsubscribe ), MqttPacketType.Unsubscribe )]
        [TestCase( "Files/Binaries/UnsubscribeAck.packet", "Files/Packets/UnsubscribeAck.json", typeof( UnsubscribeAck ), MqttPacketType.UnsubscribeAck )]
        public async Task when_managing_packet_from_source_then_succeeds( string packetPath, string jsonPath, Type packetType, MqttPacketType type )
        {
            jsonPath = Path.Combine( Environment.CurrentDirectory, jsonPath );

            IPacket packet = Packet.ReadPacket( jsonPath, packetType ) as IPacket;

            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            byte[] bytes = Packet.ReadAllBytes( packetPath );
            Mock<IFormatter> formatter = new Mock<IFormatter>();

            formatter.Setup( f => f.PacketType ).Returns( type );

            formatter
                .Setup( f => f.FormatAsync( It.Is<IPacket>( p => Convert.ChangeType( p, packetType ) == packet ) ) )
                .Returns( Task.FromResult( bytes ) );

            PacketManager packetManager = new PacketManager( formatter.Object );
            var result = await packetManager.GetBytesAsync( Monitored<IPacket>.Create( TestHelper.Monitor, packet ) );
            bytes.Should().BeEquivalentTo( result.Item );
        }

        [Theory]
        [TestCase( "Files/Binaries/Disconnect.packet", typeof( Disconnect ), MqttPacketType.Disconnect )]
        [TestCase( "Files/Binaries/PingRequest.packet", typeof( PingRequest ), MqttPacketType.PingRequest )]
        [TestCase( "Files/Binaries/PingResponse.packet", typeof( PingResponse ), MqttPacketType.PingResponse )]
        public async Task when_managing_packet_then_succeeds( string packetPath, Type packetType, MqttPacketType type )
        {
            IPacket packet = Activator.CreateInstance( packetType ) as IPacket;

            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            byte[] bytes = Packet.ReadAllBytes( packetPath );
            Mock<IFormatter> formatter = new Mock<IFormatter>();

            formatter.Setup( f => f.PacketType ).Returns( type );

            formatter
                .Setup( f => f.FormatAsync( It.Is<IPacket>( p => Convert.ChangeType( p, packetType ) == packet ) ) )
                .Returns( Task.FromResult( bytes ) );

            PacketManager packetManager = new PacketManager( formatter.Object );
            var result = await packetManager.GetBytesAsync( Monitored<IPacket>.Create( TestHelper.Monitor, packet ) );

            bytes.Should().BeEquivalentTo( result.Item );
        }

        [Theory]
        [TestCase( "Files/Binaries/Connect_Full.packet" )]
        [TestCase( "Files/Binaries/Connect_Min.packet" )]
        [TestCase( "Files/Binaries/ConnectAck.packet" )]
        [TestCase( "Files/Binaries/Disconnect.packet" )]
        [TestCase( "Files/Binaries/PingRequest.packet" )]
        [TestCase( "Files/Binaries/PingResponse.packet" )]
        [TestCase( "Files/Binaries/Publish_Full.packet" )]
        [TestCase( "Files/Binaries/Publish_Min.packet" )]
        [TestCase( "Files/Binaries/PublishAck.packet" )]
        [TestCase( "Files/Binaries/PublishComplete.packet" )]
        [TestCase( "Files/Binaries/PublishReceived.packet" )]
        [TestCase( "Files/Binaries/PublishRelease.packet" )]
        [TestCase( "Files/Binaries/Subscribe_MultiTopic.packet" )]
        [TestCase( "Files/Binaries/Subscribe_SingleTopic.packet" )]
        [TestCase( "Files/Binaries/SubscribeAck_MultiTopic.packet" )]
        [TestCase( "Files/Binaries/SubscribeAck_SingleTopic.packet" )]
        [TestCase( "Files/Binaries/Unsubscribe_MultiTopic.packet" )]
        [TestCase( "Files/Binaries/Unsubscribe_SingleTopic.packet" )]
        [TestCase( "Files/Binaries/UnsubscribeAck.packet" )]
        public void when_managing_unknown_packet_bytes_then_fails( string packetPath )
        {
            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            byte[] packet = Packet.ReadAllBytes( packetPath );
            Mock<IFormatter> formatter = new Mock<IFormatter>();
            PacketManager packetManager = new PacketManager( formatter.Object );

            AggregateException ex = Assert.Throws<AggregateException>( () => packetManager.GetPacketAsync( Monitored<byte[]>.Create( TestHelper.Monitor, packet ) ).Wait() );

            ex.InnerException.Should().BeOfType<MqttException>();
        }

        [Theory]
        [TestCase( "Files/Packets/Connect_Full.json" )]
        [TestCase( "Files/Packets/Connect_Min.json" )]
        [TestCase( "Files/Packets/ConnectAck.json" )]
        [TestCase( "Files/Packets/Publish_Full.json" )]
        [TestCase( "Files/Packets/Publish_Min.json" )]
        [TestCase( "Files/Packets/PublishAck.json" )]
        [TestCase( "Files/Packets/PublishComplete.json" )]
        [TestCase( "Files/Packets/PublishReceived.json" )]
        [TestCase( "Files/Packets/PublishRelease.json" )]
        [TestCase( "Files/Packets/Subscribe_MultiTopic.json" )]
        [TestCase( "Files/Packets/Subscribe_SingleTopic.json" )]
        [TestCase( "Files/Packets/SubscribeAck_MultiTopic.json" )]
        [TestCase( "Files/Packets/SubscribeAck_SingleTopic.json" )]
        [TestCase( "Files/Packets/Unsubscribe_MultiTopic.json" )]
        [TestCase( "Files/Packets/Unsubscribe_SingleTopic.json" )]
        [TestCase( "Files/Packets/UnsubscribeAck.json" )]
        public void when_managing_unknown_packet_then_fails( string jsonPath )
        {
            jsonPath = Path.Combine( Environment.CurrentDirectory, jsonPath );

            Connect packet = Packet.ReadPacket<Connect>( jsonPath );
            Mock<IFormatter> formatter = new Mock<IFormatter>();
            PacketManager packetManager = new PacketManager( formatter.Object );

            AggregateException ex = Assert.Throws<AggregateException>( () => packetManager.GetBytesAsync( Monitored<IPacket>.Create( TestHelper.Monitor, packet ) ).Wait() );

            Assert.True( ex.InnerException is MqttException );
        }
    }
}
