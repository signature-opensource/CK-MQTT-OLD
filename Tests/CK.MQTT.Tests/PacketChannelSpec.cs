using CK.Core;
using CK.MQTT;

using CK.MQTT.Sdk;
using CK.MQTT.Sdk.Bindings;
using CK.MQTT.Sdk.Packets;
using CK.MQTT.Tests.Files.Packets;
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
            Subject<Mon<byte[]>> receiver = new Subject<Mon<byte[]>>();
            Mock<IMqttChannel<byte[]>> bufferedChannel = new Mock<IMqttChannel<byte[]>>();

            bufferedChannel.Setup( x => x.ReceiverStream ).Returns( receiver );

            IMqttTopicEvaluator topicEvaluator = Mock.Of<IMqttTopicEvaluator>();
            PacketChannelFactory factory = new PacketChannelFactory( topicEvaluator, configuration );
            IMqttChannel<IPacket> channel = factory.Create( TestHelper.Monitor, bufferedChannel.Object );

            Assert.NotNull( channel );
        }

        static readonly object[] _cases0 = new object[]
        {
            new object[] { "Files/Binaries/Connect_Full.packet", Packets.Connect_Full },
            new object[] { "Files/Binaries/Connect_Min.packet", Packets.Connect_Min},
            new object[] { "Files/Binaries/ConnectAck.packet", Packets.ConnectAck },
            new object[] { "Files/Binaries/Publish_Full.packet", Packets.Publish_Full },
            new object[] { "Files/Binaries/Publish_Min.packet", Packets.Publish_Min },
            new object[] { "Files/Binaries/PublishAck.packet", Packets.PublishAck },
            new object[] { "Files/Binaries/PublishComplete.packet", Packets.PublishComplete },
            new object[] { "Files/Binaries/PublishReceived.packet", Packets.PublishReceived },
            new object[] { "Files/Binaries/PublishRelease.packet", Packets.PublishRelease },
            new object[] { "Files/Binaries/Subscribe_MultiTopic.packet", Packets.Subscribe_MultiTopic },
            new object[] { "Files/Binaries/Subscribe_SingleTopic.packet", Packets.Subscribe_SingleTopic },
            new object[] { "Files/Binaries/SubscribeAck_MultiTopic.packet", Packets.SubscribeAck_MultiTopic },
            new object[] { "Files/Binaries/SubscribeAck_SingleTopic.packet", Packets.SubscribeAck_SingleTopic },
            new object[] { "Files/Binaries/Unsubscribe_MultiTopic.packet", Packets.Unsubscribe_MultiTopic},
            new object[] { "Files/Binaries/Unsubscribe_SingleTopic.packet", Packets.Unsubscribe_SingleTopic },
            new object[] { "Files/Binaries/UnsubscribeAck.packet", Packets.UnsubscribeAck }
        };

        [Test, TestCaseSource( nameof( _cases0 ) )]
        public void when_reading_bytes_from_source_then_notifies_packet( string packetPath, IPacket packet )
        {
            MqttConfiguration configuration = new MqttConfiguration { WaitTimeoutSecs = 1 };
            Subject<Mon<byte[]>> receiver = new Subject<Mon<byte[]>>();

            Mock<IMqttChannel<byte[]>> innerChannel = new Mock<IMqttChannel<byte[]>>();

            innerChannel.Setup( x => x.ReceiverStream ).Returns( receiver );

            IPacket expectedPacket = packet;

            Mock<IPacketManager> manager = new Mock<IPacketManager>();

            manager.Setup( x => x.GetPacketAsync( It.IsAny<Mon<byte[]>>() ) )
                .Returns( Task.FromResult<Mon<IPacket>>( new Mon<IPacket>( TestHelper.Monitor, expectedPacket ) ) );

            PacketChannel channel = new PacketChannel(innerChannel.Object, manager.Object, configuration);

            IPacket receivedPacket = default;

            channel.ReceiverStream.Subscribe( packet =>
            {
                receivedPacket = packet.Item;
            } );

            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            byte[] readPacket = Packet.ReadAllBytes( packetPath );

            receiver.OnNext( new Mon<byte[]>( TestHelper.Monitor, readPacket ) );

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
            Subject<Mon<byte[]>> receiver = new Subject<Mon<byte[]>>();
            Mock<IMqttChannel<byte[]>> innerChannel = new Mock<IMqttChannel<byte[]>>();

            innerChannel.Setup( x => x.ReceiverStream ).Returns( receiver );

            object expectedPacket = Activator.CreateInstance( packetType );
            Mock<IPacketManager> manager = new Mock<IPacketManager>();

            manager.Setup( x => x.GetPacketAsync( It.IsAny<Mon<byte[]>>() ) )
                .Returns( Task.FromResult<Mon<IPacket>>( new Mon<IPacket>( TestHelper.Monitor, (IPacket)expectedPacket ) ) );

            PacketChannel channel = new PacketChannel(innerChannel.Object, manager.Object, configuration);

            IPacket receivedPacket = default;

            channel.ReceiverStream.Subscribe( packet =>
            {
                receivedPacket = packet.Item;
            } );

            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            byte[] readPacket = Packet.ReadAllBytes( packetPath );

            receiver.OnNext( new Mon<byte[]>( TestHelper.Monitor, readPacket ) );

            Assert.NotNull( receivedPacket );
            packetType.Should().Be( receivedPacket.GetType() );
        }

        static readonly object[] _cases1 = new object[]
        {
            new object[] { "Files/Binaries/Connect_Full.packet", Packets.Connect_Full, typeof( Connect ) },
            new object[] { "Files/Binaries/Connect_Min.packet", Packets.Connect_Min, typeof( Connect ) },
            new object[] { "Files/Binaries/ConnectAck.packet", Packets.ConnectAck, typeof( ConnectAck ) },
            new object[] { "Files/Binaries/Publish_Full.packet", Packets.Publish_Full, typeof( Publish ) },
            new object[] { "Files/Binaries/Publish_Min.packet", Packets.Publish_Min, typeof( Publish ) },
            new object[] { "Files/Binaries/PublishAck.packet", Packets.PublishAck, typeof( PublishAck ) },
            new object[] { "Files/Binaries/PublishComplete.packet", Packets.PublishComplete, typeof( PublishComplete ) },
            new object[] { "Files/Binaries/PublishReceived.packet", Packets.PublishReceived, typeof( PublishReceived ) },
            new object[] { "Files/Binaries/PublishRelease.packet", Packets.PublishRelease, typeof( PublishRelease ) },
            new object[] { "Files/Binaries/Subscribe_MultiTopic.packet", Packets.Subscribe_MultiTopic, typeof( Subscribe ) },
            new object[] { "Files/Binaries/Subscribe_SingleTopic.packet", Packets.Subscribe_SingleTopic, typeof( Subscribe ) },
            new object[] { "Files/Binaries/SubscribeAck_MultiTopic.packet", Packets.SubscribeAck_MultiTopic, typeof( SubscribeAck ) },
            new object[] { "Files/Binaries/SubscribeAck_SingleTopic.packet", Packets.SubscribeAck_SingleTopic, typeof( SubscribeAck ) },
            new object[] { "Files/Binaries/Unsubscribe_MultiTopic.packet", Packets.Unsubscribe_MultiTopic, typeof( Unsubscribe ) },
            new object[] { "Files/Binaries/Unsubscribe_SingleTopic.packet", Packets.Unsubscribe_SingleTopic, typeof( Unsubscribe ) },
            new object[] { "Files/Binaries/UnsubscribeAck.packet", Packets.UnsubscribeAck, typeof( UnsubscribeAck ) },
        };

        [Test, TestCaseSource( nameof( _cases1 ) )]
        public async Task when_writing_packet_from_source_then_inner_channel_is_notified( string packetPath, IPacket packet, Type packetType )
        {
            MqttConfiguration configuration = new MqttConfiguration { WaitTimeoutSecs = 1 };

            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            byte[] bytes = Packet.ReadAllBytes( packetPath );

            Subject<Mon<byte[]>> receiver = new Subject<Mon<byte[]>>();
            Mock<IMqttChannel<byte[]>> innerChannel = new Mock<IMqttChannel<byte[]>>();

            innerChannel.Setup( x => x.ReceiverStream ).Returns( receiver );
            innerChannel.Setup( x => x.SendAsync( It.IsAny<Mon<byte[]>>() ) )
                .Returns( Task.CompletedTask );

            Mock<IPacketManager> manager = new Mock<IPacketManager>();

            manager.Setup( x => x.GetBytesAsync( It.IsAny<Mon<IPacket>>() ) )
                .Returns( Task.FromResult<Mon<byte[]>>( new Mon<byte[]>( TestHelper.Monitor, bytes ) ) );

            PacketChannel channel = new PacketChannel(innerChannel.Object, manager.Object, configuration);

            await channel.SendAsync( new Mon<IPacket>( TestHelper.Monitor, packet ) );

            innerChannel.Verify( x => x.SendAsync( It.Is<Mon<byte[]>>( b => b.Item.ToList().SequenceEqual( bytes ) ) ) );
            manager.Verify( x => x.GetBytesAsync( It.Is<Mon<IPacket>>( p => Convert.ChangeType( p.Item, packetType ) == packet ) ) );
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

            Subject<Mon<byte[]>> receiver = new Subject<Mon<byte[]>>();
            Mock<IMqttChannel<byte[]>> innerChannel = new Mock<IMqttChannel<byte[]>>();

            innerChannel.Setup( x => x.ReceiverStream ).Returns( receiver );
            innerChannel.Setup( x => x.SendAsync( It.IsAny<Mon<byte[]>>() ) )
                .Returns( Task.CompletedTask );

            IPacket packet = Activator.CreateInstance( packetType ) as IPacket;

            Mock<IPacketManager> manager = new Mock<IPacketManager>();

            manager.Setup( x => x.GetBytesAsync( It.IsAny<Mon<IPacket>>() ) )
                .Returns( Task.FromResult<Mon<byte[]>>( new Mon<byte[]>( TestHelper.Monitor, bytes ) ) );

            PacketChannel channel = new PacketChannel(innerChannel.Object, manager.Object, configuration);

            await channel.SendAsync( new Mon<IPacket>( TestHelper.Monitor, packet ) );

            innerChannel.Verify( x => x.SendAsync( It.Is<Mon<byte[]>>( b => b.Item.ToList().SequenceEqual( bytes ) ) ) );
            manager.Verify( x => x.GetBytesAsync( It.Is<Mon<IPacket>>( p => Convert.ChangeType( p.Item, packetType ) == packet ) ) );
        }

        [Test]
        public void when_packet_channel_error_then_notifies()
        {
            MqttConfiguration configuration = new MqttConfiguration { WaitTimeoutSecs = 1 };
            Subject<Mon<byte[]>> receiver = new Subject<Mon<byte[]>>();
            Mock<IMqttChannel<byte[]>> innerChannel = new Mock<IMqttChannel<byte[]>>();

            innerChannel.Setup( x => x.ReceiverStream ).Returns( receiver );

            Mock<IPacketManager> manager = new Mock<IPacketManager>();

            PacketChannel channel = new PacketChannel(innerChannel.Object, manager.Object, configuration);

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
