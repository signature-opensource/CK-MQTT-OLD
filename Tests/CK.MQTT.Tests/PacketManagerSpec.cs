using CK.Core;
using CK.MQTT;

using CK.MQTT.Sdk;
using CK.MQTT.Sdk.Formatters;
using CK.MQTT.Sdk.Packets;
using CK.MQTT.Tests.Files.Packets;
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
        static readonly object[] _cases0 = new object[] {
            new object[] { "Files/Binaries/Connect_Full.packet", Packets.Connect_Full, typeof(Connect ), MqttPacketType.Connect },
            new object[] { "Files/Binaries/Connect_Min.packet", Packets.Connect_Min, typeof(Connect ), MqttPacketType.Connect },
            new object[] { "Files/Binaries/ConnectAck.packet", Packets.ConnectAck, typeof(ConnectAck ), MqttPacketType.ConnectAck },
            new object[] { "Files/Binaries/Publish_Full.packet", Packets.Publish_Full, typeof(Publish ), MqttPacketType.Publish },
            new object[] { "Files/Binaries/Publish_Min.packet", Packets.Publish_Min, typeof(Publish ), MqttPacketType.Publish },
            new object[] { "Files/Binaries/PublishAck.packet", Packets.PublishAck, typeof(PublishAck ), MqttPacketType.PublishAck },
            new object[] { "Files/Binaries/PublishComplete.packet", Packets.PublishComplete, typeof(PublishComplete ), MqttPacketType.PublishComplete },
            new object[] { "Files/Binaries/PublishReceived.packet", Packets.PublishReceived, typeof(PublishReceived ), MqttPacketType.PublishReceived },
            new object[] { "Files/Binaries/PublishRelease.packet", Packets.PublishRelease, typeof(PublishRelease ), MqttPacketType.PublishRelease },
            new object[] { "Files/Binaries/Subscribe_MultiTopic.packet", Packets.Subscribe_MultiTopic, typeof(Subscribe ), MqttPacketType.Subscribe },
            new object[] { "Files/Binaries/Subscribe_SingleTopic.packet", Packets.Subscribe_SingleTopic, typeof(Subscribe ), MqttPacketType.Subscribe },
            new object[] { "Files/Binaries/SubscribeAck_MultiTopic.packet", Packets.SubscribeAck_MultiTopic, typeof(SubscribeAck ), MqttPacketType.SubscribeAck },
            new object[] { "Files/Binaries/SubscribeAck_SingleTopic.packet", Packets.SubscribeAck_SingleTopic, typeof(SubscribeAck ), MqttPacketType.SubscribeAck },
            new object[] { "Files/Binaries/Unsubscribe_MultiTopic.packet", Packets.Unsubscribe_MultiTopic, typeof(Unsubscribe ), MqttPacketType.Unsubscribe },
            new object[] { "Files/Binaries/Unsubscribe_SingleTopic.packet", Packets.Unsubscribe_SingleTopic, typeof(Unsubscribe ), MqttPacketType.Unsubscribe },
            new object[] { "Files/Binaries/UnsubscribeAck.packet", Packets.UnsubscribeAck, typeof(UnsubscribeAck ), MqttPacketType.UnsubscribeAck },
        };

        [Test, TestCaseSource( nameof( _cases0 ) )]
        public async Task when_managing_packet_bytes_then_succeeds( string packetPath, IPacket packet, Type packetType, MqttPacketType type )
        {
            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            byte[] bytes = Packet.ReadAllBytes( packetPath );

            Mock<IFormatter> formatter = new Mock<IFormatter>();

            formatter.Setup( f => f.PacketType ).Returns( type );

            formatter
                .Setup( f => f.FormatAsync( It.Is<byte[]>( b => b.ToList().SequenceEqual( bytes ) ) ) )
                .Returns( Task.FromResult( packet ) );

            PacketManager packetManager = new PacketManager( formatter.Object );
            var result = await packetManager.GetPacketAsync( new Mon<byte[]>( TestHelper.Monitor, bytes ) );
            packet.Should().Be( result.Item );
        }
        static readonly object[] _cases1 = new object[]
        {
            new object[] { "Files/Binaries/Connect_Full.packet", Packets.Connect_Full, typeof( Connect ), MqttPacketType.Connect },
            new object[] { "Files/Binaries/Connect_Min.packet", Packets.Connect_Min, typeof( Connect ), MqttPacketType.Connect },
            new object[] { "Files/Binaries/ConnectAck.packet", Packets.ConnectAck, typeof( ConnectAck ), MqttPacketType.ConnectAck },
            new object[] { "Files/Binaries/Publish_Full.packet", Packets.Publish_Full, typeof( Publish ), MqttPacketType.Publish },
            new object[] { "Files/Binaries/Publish_Min.packet", Packets.Publish_Min, typeof( Publish ), MqttPacketType.Publish },
            new object[] { "Files/Binaries/PublishAck.packet", Packets.PublishAck, typeof( PublishAck ), MqttPacketType.PublishAck },
            new object[] { "Files/Binaries/PublishComplete.packet", Packets.PublishComplete, typeof( PublishComplete ), MqttPacketType.PublishComplete },
            new object[] { "Files/Binaries/PublishReceived.packet", Packets.PublishReceived, typeof( PublishReceived ), MqttPacketType.PublishReceived },
            new object[] { "Files/Binaries/PublishRelease.packet", Packets.PublishRelease, typeof( PublishRelease ), MqttPacketType.PublishRelease },
            new object[] { "Files/Binaries/Subscribe_MultiTopic.packet", Packets.Subscribe_MultiTopic, typeof( Subscribe ), MqttPacketType.Subscribe },
            new object[] { "Files/Binaries/Subscribe_SingleTopic.packet", Packets.Subscribe_SingleTopic, typeof( Subscribe ), MqttPacketType.Subscribe },
            new object[] { "Files/Binaries/SubscribeAck_MultiTopic.packet", Packets.SubscribeAck_MultiTopic, typeof( SubscribeAck ), MqttPacketType.SubscribeAck },
            new object[] { "Files/Binaries/SubscribeAck_SingleTopic.packet", Packets.SubscribeAck_SingleTopic, typeof( SubscribeAck ), MqttPacketType.SubscribeAck },
            new object[] { "Files/Binaries/Unsubscribe_MultiTopic.packet", Packets.Unsubscribe_MultiTopic, typeof( Unsubscribe ), MqttPacketType.Unsubscribe },
            new object[] { "Files/Binaries/Unsubscribe_SingleTopic.packet", Packets.Unsubscribe_SingleTopic, typeof( Unsubscribe ), MqttPacketType.Unsubscribe },
            new object[] { "Files/Binaries/UnsubscribeAck.packet", Packets.UnsubscribeAck, typeof( UnsubscribeAck ), MqttPacketType.UnsubscribeAck },
        };

        [Test, TestCaseSource( nameof( _cases1 ) )]
        public async Task when_managing_packet_from_source_then_succeeds( string packetPath, IPacket packet, Type packetType, MqttPacketType type )
        {
            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            byte[] bytes = Packet.ReadAllBytes( packetPath );
            Mock<IFormatter> formatter = new Mock<IFormatter>();

            formatter.Setup( f => f.PacketType ).Returns( type );

            formatter
                .Setup( f => f.FormatAsync( It.Is<IPacket>( p => Convert.ChangeType( p, packetType ) == packet ) ) )
                .Returns( Task.FromResult( bytes ) );

            PacketManager packetManager = new PacketManager( formatter.Object );
            var result = await packetManager.GetBytesAsync( new Mon<IPacket>( TestHelper.Monitor, packet ) );
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
            var result = await packetManager.GetBytesAsync( new Mon<IPacket>( TestHelper.Monitor, packet ) );

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

            AggregateException ex = Assert.Throws<AggregateException>( () => packetManager.GetPacketAsync( new Mon<byte[]>( TestHelper.Monitor, packet ) ).Wait() );

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

            AggregateException ex = Assert.Throws<AggregateException>( () => packetManager.GetBytesAsync( new Mon<IPacket>( TestHelper.Monitor, packet ) ).Wait() );

            Assert.True( ex.InnerException is MqttException );
        }
    }
}
