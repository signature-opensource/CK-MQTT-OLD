using CK.Core;
using CK.MQTT;

using CK.MQTT.Sdk;
using CK.MQTT.Sdk.Flows;
using CK.MQTT.Sdk.Packets;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

using static CK.Testing.MonitorTestHelper;

namespace Tests
{
    public class ServerSpec
    {
        [Test]
        public void when_server_does_not_start_then_connections_are_ignored()
        {
            var sockets = new Subject<Mon<IMqttChannel<byte[]>>>();
            Mock<IMqttChannelListener> channelProvider = new Mock<IMqttChannelListener>();

            channelProvider
                .Setup( p => p.ChannelStream )
                .Returns( sockets );

            MqttConfiguration configuration = Mock.Of<MqttConfiguration>( c => c.WaitTimeoutSecs == 60 );

            Subject<Mon<IPacket>> packets = new Subject<Mon<IPacket>>();
            Mock<IMqttChannel<IPacket>> packetChannel = new Mock<IMqttChannel<IPacket>>();
            IPacketChannelFactory factory = Mock.Of<IPacketChannelFactory>( x => x.Create( TestHelper.Monitor, It.IsAny<IMqttChannel<byte[]>>() ) == packetChannel.Object );

            packetChannel
                .Setup( c => c.IsConnected )
                .Returns( true );
            packetChannel
                .Setup( c => c.SenderStream )
                .Returns( new Subject<Mon<IPacket>>() );
            packetChannel
                .Setup( c => c.ReceiverStream )
                .Returns( packets );

            IProtocolFlowProvider flowProvider = Mock.Of<IProtocolFlowProvider>();
            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();

            MqttServerImpl server = new MqttServerImpl( TestHelper.Monitor, channelProvider.Object, factory, flowProvider, connectionProvider.Object, Mock.Of<ISubject<MqttUndeliveredMessage>>(), configuration );

            sockets.OnNext( new Mon<IMqttChannel<byte[]>>( TestHelper.Monitor, Mock.Of<IMqttChannel<byte[]>>( x => x.ReceiverStream == new Subject<Mon<byte[]>>() ) ) );
            sockets.OnNext( new Mon<IMqttChannel<byte[]>>( TestHelper.Monitor, Mock.Of<IMqttChannel<byte[]>>( x => x.ReceiverStream == new Subject<Mon<byte[]>>() ) ) );
            sockets.OnNext( new Mon<IMqttChannel<byte[]>>( TestHelper.Monitor, Mock.Of<IMqttChannel<byte[]>>( x => x.ReceiverStream == new Subject<Mon<byte[]>>() ) ) );

            0.Should().Be( server.ActiveConnections );
        }

        [Test]
        public void when_connection_established_then_active_connections_increases()
        {
            var sockets = new Subject<Mon<IMqttChannel<byte[]>>>();
            Mock<IMqttChannelListener> channelProvider = new Mock<IMqttChannelListener>();

            channelProvider
                .Setup( p => p.ChannelStream )
                .Returns( sockets );

            MqttConfiguration configuration = Mock.Of<MqttConfiguration>( c => c.WaitTimeoutSecs == 60 );

            Subject<Mon<IPacket>> packets = new Subject<Mon<IPacket>>();
            Mock<IMqttChannel<IPacket>> packetChannel = new Mock<IMqttChannel<IPacket>>();
            IPacketChannelFactory factory = Mock.Of<IPacketChannelFactory>( x => x.Create( TestHelper.Monitor, It.IsAny<IMqttChannel<byte[]>>() ) == packetChannel.Object );

            packetChannel
                .Setup( c => c.IsConnected )
                .Returns( true );
            packetChannel
                .Setup( c => c.SenderStream )
                .Returns( new Subject<Mon<IPacket>>() );
            packetChannel
                .Setup( c => c.ReceiverStream )
                .Returns( packets );

            IProtocolFlowProvider flowProvider = Mock.Of<IProtocolFlowProvider>();
            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();

            MqttServerImpl server = new MqttServerImpl( TestHelper.Monitor, channelProvider.Object, factory, flowProvider, connectionProvider.Object, Mock.Of<ISubject<MqttUndeliveredMessage>>(), configuration );

            server.Start();

            sockets.OnNext( new Mon<IMqttChannel<byte[]>>( TestHelper.Monitor, Mock.Of<IMqttChannel<byte[]>>( x => x.ReceiverStream == new Subject<Mon<byte[]>>() ) ) );
            sockets.OnNext( new Mon<IMqttChannel<byte[]>>( TestHelper.Monitor, Mock.Of<IMqttChannel<byte[]>>( x => x.ReceiverStream == new Subject<Mon<byte[]>>() ) ) );
            sockets.OnNext( new Mon<IMqttChannel<byte[]>>( TestHelper.Monitor, Mock.Of<IMqttChannel<byte[]>>( x => x.ReceiverStream == new Subject<Mon<byte[]>>() ) ) );

            server.ActiveConnections.Should().Be( 3 );
        }

        [Test]
        public void when_server_closed_then_pending_connection_is_closed()
        {
            var sockets = new Subject<Mon<IMqttChannel<byte[]>>>();
            Mock<IMqttChannelListener> channelProvider = new Mock<IMqttChannelListener>();

            channelProvider
                .Setup( p => p.ChannelStream )
                .Returns( sockets );

            Mock<IMqttChannel<IPacket>> packetChannel = new Mock<IMqttChannel<IPacket>>();

            packetChannel
                .Setup( c => c.IsConnected )
                .Returns( true );
            packetChannel
                .Setup( c => c.SenderStream )
                .Returns( new Subject<Mon<IPacket>>() );
            packetChannel
                .Setup( c => c.ReceiverStream )
                .Returns( new Subject<Mon<IPacket>>() );

            IProtocolFlowProvider flowProvider = Mock.Of<IProtocolFlowProvider>();
            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();

            MqttConfiguration configuration = Mock.Of<MqttConfiguration>( c => c.WaitTimeoutSecs == 60 );

            MqttServerImpl server = new MqttServerImpl( TestHelper.Monitor, channelProvider.Object, Mock.Of<IPacketChannelFactory>( x => x.Create( TestHelper.Monitor, It.IsAny<IMqttChannel<byte[]>>() ) == packetChannel.Object ),
                flowProvider, connectionProvider.Object, Mock.Of<ISubject<MqttUndeliveredMessage>>(), configuration );

            server.Start();

            Mock<IMqttChannel<byte[]>> socket = new Mock<IMqttChannel<byte[]>>();

            sockets.OnNext( new Mon<IMqttChannel<byte[]>>( TestHelper.Monitor, socket.Object ) );

            server.Stop();

            packetChannel.Verify( x => x.Dispose() );
        }

        [Test]
        public async Task when_receiver_error_then_closes_connection()
        {
            var sockets = new Subject<Mon<IMqttChannel<byte[]>>>();
            Mock<IMqttChannelListener> channelProvider = new Mock<IMqttChannelListener>();

            channelProvider
                .Setup( p => p.ChannelStream )
                .Returns( sockets );

            MqttConfiguration configuration = Mock.Of<MqttConfiguration>( c => c.WaitTimeoutSecs == 60 );

            Subject<Mon<IPacket>> packets = new Subject<Mon<IPacket>>();

            Mock<IMqttChannel<IPacket>> packetChannel = new Mock<IMqttChannel<IPacket>>();
            Mock<IPacketChannelFactory> factory = new Mock<IPacketChannelFactory>();

            packetChannel
                .Setup( c => c.IsConnected )
                .Returns( true );
            packetChannel
                .Setup( c => c.SenderStream )
                .Returns( new Subject<Mon<IPacket>>() );
            packetChannel
                .Setup( c => c.ReceiverStream )
                .Returns( packets );

            factory.Setup( x => x.Create( TestHelper.Monitor, It.IsAny<IMqttChannel<byte[]>>() ) )
                .Returns( packetChannel.Object );

            IProtocolFlowProvider flowProvider = Mock.Of<IProtocolFlowProvider>();
            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();

            MqttServerImpl server = new MqttServerImpl( TestHelper.Monitor, channelProvider.Object, factory.Object, flowProvider, connectionProvider.Object, Mock.Of<ISubject<MqttUndeliveredMessage>>(), configuration );
            Subject<Mon<byte[]>> receiver = new Subject<Mon<byte[]>>();
            Mock<IMqttChannel<byte[]>> socket = new Mock<IMqttChannel<byte[]>>();

            socket.Setup( x => x.ReceiverStream ).Returns( receiver );

            server.Start();

            sockets.OnNext( new Mon<IMqttChannel<byte[]>>( TestHelper.Monitor, socket.Object ) );

            try
            {
                packets.OnError( new Exception( "Protocol exception" ) );
            }
            catch( Exception )
            {
            }

            await Task.Delay( TimeSpan.FromMilliseconds( 1000 ) );

            packetChannel.Verify( x => x.Dispose() );
        }
    }
}
