using System;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using CK.MQTT;
using CK.MQTT.Sdk.Packets;
using Moq;
using CK.MQTT.Sdk;
using FluentAssertions;
using static CK.Testing.MonitorTestHelper;
using NUnit.Framework;
using CK.Core;

namespace Tests
{
    public class PacketChannelSpec
    {
        [Test]
        public void when_creating_packet_channel_then_succeeds()
        {
            var configuration = new MqttConfiguration { WaitTimeoutSecs = 1 };
            var receiver = new Subject<(IActivityMonitor, byte[])>();
            var bufferedChannel = new Mock<IMqttChannel<byte[]>>();

            bufferedChannel.Setup( x => x.ReceiverStream ).Returns( receiver );

            var topicEvaluator = Mock.Of<IMqttTopicEvaluator>();
            var factory = new PacketChannelFactory( topicEvaluator, configuration );
            var channel = factory.Create( bufferedChannel.Object );

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
            var configuration = new MqttConfiguration { WaitTimeoutSecs = 1 };
            var receiver = new Subject<(IActivityMonitor, byte[])>();
            var innerChannel = new Mock<IMqttChannel<byte[]>>();

            innerChannel.Setup( x => x.ReceiverStream ).Returns( receiver );

            jsonPath = Path.Combine( Environment.CurrentDirectory, jsonPath );

            var expectedPacket = Packet.ReadPacket( jsonPath, packetType ) as IPacket;

            var manager = new Mock<IPacketManager>();

            manager.Setup( x => x.GetPacketAsync( It.IsAny<byte[]>() ) )
                .Returns( Task.FromResult<IPacket>( expectedPacket ) );

            var channel = new PacketChannel( innerChannel.Object, manager.Object, configuration );

            var receivedPacket = default( IPacket );

            channel.ReceiverStream.Subscribe( packet =>
            {
                receivedPacket = packet;
            } );

            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            var readPacket = Packet.ReadAllBytes( packetPath );

            receiver.OnNext( (TestHelper.Monitor, readPacket) );

            Assert.NotNull( receivedPacket );
            expectedPacket.Should().Be( receivedPacket );
        }

        [Theory]
        [TestCase( "Files/Binaries/Disconnect.packet", typeof( Disconnect ) )]
        [TestCase( "Files/Binaries/PingRequest.packet", typeof( PingRequest ) )]
        [TestCase( "Files/Binaries/PingResponse.packet", typeof( PingResponse ) )]
        public void when_reading_bytes_then_notifies_packet( string packetPath, Type packetType )
        {
            var configuration = new MqttConfiguration { WaitTimeoutSecs = 1 };
            var receiver = new Subject<(IActivityMonitor, byte[])>();
            var innerChannel = new Mock<IMqttChannel<byte[]>>();

            innerChannel.Setup( x => x.ReceiverStream ).Returns( receiver );

            var expectedPacket = Activator.CreateInstance( packetType );
            var manager = new Mock<IPacketManager>();

            manager.Setup( x => x.GetPacketAsync( It.IsAny<byte[]>() ) )
                .Returns( Task.FromResult<IPacket>( (IPacket)expectedPacket ) );

            var channel = new PacketChannel( innerChannel.Object, manager.Object, configuration );

            var receivedPacket = default( IPacket );

            channel.ReceiverStream.Subscribe( packet =>
            {
                receivedPacket = packet;
            } );

            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            var readPacket = Packet.ReadAllBytes( packetPath );

            receiver.OnNext( (TestHelper.Monitor, readPacket) );

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
            var configuration = new MqttConfiguration { WaitTimeoutSecs = 1 };

            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            var bytes = Packet.ReadAllBytes( packetPath );

            var receiver = new Subject<(IActivityMonitor, byte[])>();
            var innerChannel = new Mock<IMqttChannel<byte[]>>();

            innerChannel.Setup( x => x.ReceiverStream ).Returns( receiver );
            innerChannel.Setup( x => x.SendAsync( TestHelper.Monitor, It.IsAny<byte[]>() ) )
                .Returns( Task.Delay( 0 ) );

            jsonPath = Path.Combine( Environment.CurrentDirectory, jsonPath );

            var packet = Packet.ReadPacket( jsonPath, packetType ) as IPacket;

            var manager = new Mock<IPacketManager>();

            manager.Setup( x => x.GetBytesAsync( It.IsAny<IPacket>() ) )
                .Returns( Task.FromResult( bytes ) );

            var channel = new PacketChannel( innerChannel.Object, manager.Object, configuration );

            await channel.SendAsync( packet )
                .ConfigureAwait( continueOnCapturedContext: false );

            innerChannel.Verify( x => x.SendAsync( TestHelper.Monitor, It.Is<byte[]>( b => b.ToList().SequenceEqual( bytes ) ) ) );
            manager.Verify( x => x.GetBytesAsync( It.Is<IPacket>( p => Convert.ChangeType( p, packetType ) == packet ) ) );
        }

        [Theory]
        [TestCase( "Files/Binaries/Disconnect.packet", typeof( Disconnect ) )]
        [TestCase( "Files/Binaries/PingRequest.packet", typeof( PingRequest ) )]
        [TestCase( "Files/Binaries/PingResponse.packet", typeof( PingResponse ) )]
        public async Task when_writing_packet_then_inner_channel_is_notified( string packetPath, Type packetType )
        {
            var configuration = new MqttConfiguration { WaitTimeoutSecs = 1 };

            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            var bytes = Packet.ReadAllBytes( packetPath );

            var receiver = new Subject<(IActivityMonitor, byte[])>();
            var innerChannel = new Mock<IMqttChannel<byte[]>>();

            innerChannel.Setup( x => x.ReceiverStream ).Returns( receiver );
            innerChannel.Setup( x => x.SendAsync( TestHelper.Monitor, It.IsAny<byte[]>() ) )
                .Returns( Task.Delay( 0 ) );

            var packet = Activator.CreateInstance( packetType ) as IPacket;

            var manager = new Mock<IPacketManager>();

            manager.Setup( x => x.GetBytesAsync( It.IsAny<IPacket>() ) )
                .Returns( Task.FromResult( bytes ) );

            var channel = new PacketChannel( innerChannel.Object, manager.Object, configuration );

            await channel.SendAsync( packet )
                .ConfigureAwait( continueOnCapturedContext: false );

            innerChannel.Verify( x => x.SendAsync( TestHelper.Monitor, It.Is<byte[]>( b => b.ToList().SequenceEqual( bytes ) ) ) );
            manager.Verify( x => x.GetBytesAsync( It.Is<IPacket>( p => Convert.ChangeType( p, packetType ) == packet ) ) );
        }

        [Test]
        public void when_packet_channel_error_then_notifies()
        {
            var configuration = new MqttConfiguration { WaitTimeoutSecs = 1 };
            var receiver = new Subject<(IActivityMonitor, byte[])>();
            var innerChannel = new Mock<IMqttChannel<byte[]>>();

            innerChannel.Setup( x => x.ReceiverStream ).Returns( receiver );

            var manager = new Mock<IPacketManager>();

            var channel = new PacketChannel( innerChannel.Object, manager.Object, configuration );

            var errorMessage = "Packet Exception";

            receiver.OnError( new MqttException( errorMessage ) );

            var errorReceived = default( Exception );

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
