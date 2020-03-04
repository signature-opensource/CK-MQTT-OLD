using CK.Core;
using CK.MQTT;

using CK.MQTT.Sdk;
using CK.MQTT.Sdk.Packets;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

namespace Tests
{
    public class PacketChannelSpec
    {
        [Test]
        public void when_creating_packet_channel_then_succeeds()
        {
            MqttConfiguration configuration = new MqttConfiguration { WaitTimeoutSecs = 1 };
            Subject<IMonitored<byte[]>> receiver = new Subject<IMonitored<byte[]>>();
            Mock<IMqttChannel<byte[]>> bufferedChannel = new Mock<IMqttChannel<byte[]>>();

            bufferedChannel.Setup( x => x.ReceiverStream ).Returns( receiver );

            IMqttTopicEvaluator topicEvaluator = Mock.Of<IMqttTopicEvaluator>();
            PacketChannelFactory factory = new PacketChannelFactory( topicEvaluator, configuration );
            IMqttChannel<IPacket> channel = factory.Create( TestHelper.Monitor, bufferedChannel.Object );

            Assert.NotNull( channel );
        }

        [Theory]
        [TestCase( "Files/Binaries/Connect_Full.packet", "Files/Packets/Connect_Full.json", typeof( Connect ) )]
        [TestCase( "Files/Binaries/Connect_Min.packet", "Files/Packets/Connect_Min.json", typeof( Connect ) )]
        [TestCase( "Files/Binaries/ConnectAck.packet", "Files/Packets/ConnectAck.json", typeof( ConnectAck ) )]
        [TestCase( "Files/Binaries/Publish_Full.packet", "Files/Packets/Publish_Full.json", typeof( Publish ) )]
        [TestCase( "Files/Binaries/Publish_Min.packet", "Files/Packets/Publish_Min.json", typeof( Publish ) )]
        [TestCase( "Files/Binaries/PublishAck.packet", "Files/Packets/PublishAck.json", typeof( PublishAck ) )]
        [TestCase( "Files/Binaries/PublishComplete.packet", "Files/Packets/PublishComplete.json", typeof( PublishComplete ) )]
        [TestCase( "Files/Binaries/PublishReceived.packet", "Files/Packets/PublishReceived.json", typeof( PublishReceived ) )]
        [TestCase( "Files/Binaries/PublishRelease.packet", "Files/Packets/PublishRelease.json", typeof( PublishRelease ) )]
        [TestCase( "Files/Binaries/Subscribe_MultiTopic.packet", "Files/Packets/Subscribe_MultiTopic.json", typeof( Subscribe ) )]
        [TestCase( "Files/Binaries/Subscribe_SingleTopic.packet", "Files/Packets/Subscribe_SingleTopic.json", typeof( Subscribe ) )]
        [TestCase( "Files/Binaries/SubscribeAck_MultiTopic.packet", "Files/Packets/SubscribeAck_MultiTopic.json", typeof( SubscribeAck ) )]
        [TestCase( "Files/Binaries/SubscribeAck_SingleTopic.packet", "Files/Packets/SubscribeAck_SingleTopic.json", typeof( SubscribeAck ) )]
        [TestCase( "Files/Binaries/Unsubscribe_MultiTopic.packet", "Files/Packets/Unsubscribe_MultiTopic.json", typeof( Unsubscribe ) )]
        [TestCase( "Files/Binaries/Unsubscribe_SingleTopic.packet", "Files/Packets/Unsubscribe_SingleTopic.json", typeof( Unsubscribe ) )]
        [TestCase( "Files/Binaries/UnsubscribeAck.packet", "Files/Packets/UnsubscribeAck.json", typeof( UnsubscribeAck ) )]
        public void when_reading_bytes_from_source_then_notifies_packet( string packetPath, string jsonPath, Type packetType )
        {
            MqttConfiguration configuration = new MqttConfiguration { WaitTimeoutSecs = 1 };
            Subject<IMonitored<byte[]>> receiver = new Subject<IMonitored<byte[]>>();
            Mock<IMqttChannel<byte[]>> innerChannel = new Mock<IMqttChannel<byte[]>>();

            innerChannel.Setup( x => x.ReceiverStream ).Returns( receiver );

            jsonPath = Path.Combine( Environment.CurrentDirectory, jsonPath );

            IPacket expectedPacket = Packet.ReadPacket( jsonPath, packetType ) as IPacket;

            Mock<IPacketManager> manager = new Mock<IPacketManager>();

            manager.Setup( x => x.GetPacketAsync( It.IsAny<Monitored<byte[]>>() ) )
                .Returns( Task.FromResult<IMonitored<IPacket>>( Monitored<IPacket>.Create( TestHelper.Monitor, expectedPacket ) ) );

            PacketChannel channel = new PacketChannel( TestHelper.Monitor, innerChannel.Object, manager.Object, configuration );

            IPacket receivedPacket = default;

            channel.ReceiverStream.Subscribe( packet =>
            {
                receivedPacket = packet.Item;
            } );

            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            byte[] readPacket = Packet.ReadAllBytes( packetPath );

            receiver.OnNext( Monitored<byte[]>.Create( TestHelper.Monitor, readPacket ) );

            Assert.NotNull( receivedPacket );
            expectedPacket.Should().Be( receivedPacket );
        }

        [Theory]
        [TestCase( "Files/Binaries/Disconnect.packet", typeof( Disconnect ) )]
        [TestCase( "Files/Binaries/PingRequest.packet", typeof( PingRequest ) )]
        [TestCase( "Files/Binaries/PingResponse.packet", typeof( PingResponse ) )]
        public void when_reading_bytes_then_notifies_packet( string packetPath, Type packetType )
        {
            MqttConfiguration configuration = new MqttConfiguration { WaitTimeoutSecs = 1 };
            Subject<IMonitored<byte[]>> receiver = new Subject<IMonitored<byte[]>>();
            Mock<IMqttChannel<byte[]>> innerChannel = new Mock<IMqttChannel<byte[]>>();

            innerChannel.Setup( x => x.ReceiverStream ).Returns( receiver );

            object expectedPacket = Activator.CreateInstance( packetType );
            Mock<IPacketManager> manager = new Mock<IPacketManager>();

            manager.Setup( x => x.GetPacketAsync( It.IsAny<Monitored<byte[]>>() ) )
                .Returns( Task.FromResult<IMonitored<IPacket>>( Monitored<IPacket>.Create( TestHelper.Monitor, (IPacket)expectedPacket ) ) );

            PacketChannel channel = new PacketChannel( TestHelper.Monitor, innerChannel.Object, manager.Object, configuration );

            IPacket receivedPacket = default;

            channel.ReceiverStream.Subscribe( packet =>
            {
                receivedPacket = packet.Item;
            } );

            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            byte[] readPacket = Packet.ReadAllBytes( packetPath );

            receiver.OnNext( Monitored<byte[]>.Create( TestHelper.Monitor, readPacket ) );

            Assert.NotNull( receivedPacket );
            packetType.Should().Be( receivedPacket.GetType() );
        }

        [Theory]
        [TestCase( "Files/Binaries/Connect_Full.packet", "Files/Packets/Connect_Full.json", typeof( Connect ) )]
        [TestCase( "Files/Binaries/Connect_Min.packet", "Files/Packets/Connect_Min.json", typeof( Connect ) )]
        [TestCase( "Files/Binaries/ConnectAck.packet", "Files/Packets/ConnectAck.json", typeof( ConnectAck ) )]
        [TestCase( "Files/Binaries/Publish_Full.packet", "Files/Packets/Publish_Full.json", typeof( Publish ) )]
        [TestCase( "Files/Binaries/Publish_Min.packet", "Files/Packets/Publish_Min.json", typeof( Publish ) )]
        [TestCase( "Files/Binaries/PublishAck.packet", "Files/Packets/PublishAck.json", typeof( PublishAck ) )]
        [TestCase( "Files/Binaries/PublishComplete.packet", "Files/Packets/PublishComplete.json", typeof( PublishComplete ) )]
        [TestCase( "Files/Binaries/PublishReceived.packet", "Files/Packets/PublishReceived.json", typeof( PublishReceived ) )]
        [TestCase( "Files/Binaries/PublishRelease.packet", "Files/Packets/PublishRelease.json", typeof( PublishRelease ) )]
        [TestCase( "Files/Binaries/Subscribe_MultiTopic.packet", "Files/Packets/Subscribe_MultiTopic.json", typeof( Subscribe ) )]
        [TestCase( "Files/Binaries/Subscribe_SingleTopic.packet", "Files/Packets/Subscribe_SingleTopic.json", typeof( Subscribe ) )]
        [TestCase( "Files/Binaries/SubscribeAck_MultiTopic.packet", "Files/Packets/SubscribeAck_MultiTopic.json", typeof( SubscribeAck ) )]
        [TestCase( "Files/Binaries/SubscribeAck_SingleTopic.packet", "Files/Packets/SubscribeAck_SingleTopic.json", typeof( SubscribeAck ) )]
        [TestCase( "Files/Binaries/Unsubscribe_MultiTopic.packet", "Files/Packets/Unsubscribe_MultiTopic.json", typeof( Unsubscribe ) )]
        [TestCase( "Files/Binaries/Unsubscribe_SingleTopic.packet", "Files/Packets/Unsubscribe_SingleTopic.json", typeof( Unsubscribe ) )]
        [TestCase( "Files/Binaries/UnsubscribeAck.packet", "Files/Packets/UnsubscribeAck.json", typeof( UnsubscribeAck ) )]
        public async Task when_writing_packet_from_source_then_inner_channel_is_notified( string packetPath, string jsonPath, Type packetType )
        {
            MqttConfiguration configuration = new MqttConfiguration { WaitTimeoutSecs = 1 };

            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            byte[] bytes = Packet.ReadAllBytes( packetPath );

            Subject<IMonitored<byte[]>> receiver = new Subject<IMonitored<byte[]>>();
            Mock<IMqttChannel<byte[]>> innerChannel = new Mock<IMqttChannel<byte[]>>();

            innerChannel.Setup( x => x.ReceiverStream ).Returns( receiver );
            innerChannel.Setup( x => x.SendAsync( It.IsAny<Monitored<byte[]>>() ) )
                .Returns( Task.Delay( 0 ) );

            jsonPath = Path.Combine( Environment.CurrentDirectory, jsonPath );

            IPacket packet = Packet.ReadPacket( jsonPath, packetType ) as IPacket;

            Mock<IPacketManager> manager = new Mock<IPacketManager>();

            manager.Setup( x => x.GetBytesAsync( It.IsAny<Monitored<IPacket>>() ) )
                .Returns( Task.FromResult<IMonitored<byte[]>>( Monitored<byte[]>.Create( TestHelper.Monitor, bytes ) ) );

            PacketChannel channel = new PacketChannel( TestHelper.Monitor, innerChannel.Object, manager.Object, configuration );

            await channel.SendAsync( Monitored<IPacket>.Create( TestHelper.Monitor, packet ) );

            innerChannel.Verify( x => x.SendAsync( It.Is<Monitored<byte[]>>( b => b.Item.ToList().SequenceEqual( bytes ) ) ) );
            manager.Verify( x => x.GetBytesAsync( It.Is<Monitored<IPacket>>( p => Convert.ChangeType( p.Item, packetType ) == packet ) ) );
        }

        [Theory]
        [TestCase( "Files/Binaries/Disconnect.packet", typeof( Disconnect ) )]
        [TestCase( "Files/Binaries/PingRequest.packet", typeof( PingRequest ) )]
        [TestCase( "Files/Binaries/PingResponse.packet", typeof( PingResponse ) )]
        public async Task when_writing_packet_then_inner_channel_is_notified( string packetPath, Type packetType )
        {
            MqttConfiguration configuration = new MqttConfiguration { WaitTimeoutSecs = 1 };

            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            byte[] bytes = Packet.ReadAllBytes( packetPath );

            Subject<IMonitored<byte[]>> receiver = new Subject<IMonitored<byte[]>>();
            Mock<IMqttChannel<byte[]>> innerChannel = new Mock<IMqttChannel<byte[]>>();

            innerChannel.Setup( x => x.ReceiverStream ).Returns( receiver );
            innerChannel.Setup( x => x.SendAsync( It.IsAny<Monitored<byte[]>>() ) )
                .Returns( Task.Delay( 0 ) );

            IPacket packet = Activator.CreateInstance( packetType ) as IPacket;

            Mock<IPacketManager> manager = new Mock<IPacketManager>();

            manager.Setup( x => x.GetBytesAsync( It.IsAny<Monitored<IPacket>>() ) )
                .Returns( Task.FromResult<IMonitored<byte[]>>( Monitored<byte[]>.Create( TestHelper.Monitor, bytes ) ) );

            PacketChannel channel = new PacketChannel( TestHelper.Monitor, innerChannel.Object, manager.Object, configuration );

            await channel.SendAsync( Monitored<IPacket>.Create( TestHelper.Monitor, packet ) );

            innerChannel.Verify( x => x.SendAsync( It.Is<Monitored<byte[]>>( b => b.Item.ToList().SequenceEqual( bytes ) ) ) );
            manager.Verify( x => x.GetBytesAsync( It.Is<Monitored<IPacket>>( p => Convert.ChangeType( p.Item, packetType ) == packet ) ) );
        }

        [Test]
        public void when_packet_channel_error_then_notifies()
        {
            MqttConfiguration configuration = new MqttConfiguration { WaitTimeoutSecs = 1 };
            Subject<IMonitored<byte[]>> receiver = new Subject<IMonitored<byte[]>>();
            Mock<IMqttChannel<byte[]>> innerChannel = new Mock<IMqttChannel<byte[]>>();

            innerChannel.Setup( x => x.ReceiverStream ).Returns( receiver );

            Mock<IPacketManager> manager = new Mock<IPacketManager>();

            PacketChannel channel = new PacketChannel( TestHelper.Monitor, innerChannel.Object, manager.Object, configuration );

            string errorMessage = "Packet Exception";

            receiver.OnError( new MqttException( errorMessage ) );

            Exception errorReceived = default;

            channel.ReceiverStream.Subscribe( _ => { }, ex =>
            {
                errorReceived = ex;
            } );

            Assert.NotNull( errorReceived );
            Assert.True( errorReceived is MqttException );
            errorMessage.Should().Be( (errorReceived as MqttException).Message );
        }
    }
}
