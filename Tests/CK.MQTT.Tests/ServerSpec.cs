using CK.MQTT;
using CK.MQTT.Sdk;
using CK.MQTT.Sdk.Flows;
using CK.MQTT.Sdk.Packets;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using System;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace Tests
{
    public class ServerSpec
    {
        [Test]
        public void when_server_does_not_start_then_connections_are_ignored()
        {
            Subject<IMqttChannel<byte[]>> sockets = new Subject<IMqttChannel<byte[]>>();
            Mock<IMqttChannelListener> channelProvider = new Mock<IMqttChannelListener>();

            channelProvider
                .Setup( p => p.GetChannelStream() )
                .Returns( sockets );

            MqttConfiguration configuration = Mock.Of<MqttConfiguration>( c => c.WaitTimeoutSecs == 60 );

            Subject<IPacket> packets = new Subject<IPacket>();
            Mock<IMqttChannel<IPacket>> packetChannel = new Mock<IMqttChannel<IPacket>>();
            IPacketChannelFactory factory = Mock.Of<IPacketChannelFactory>( x => x.Create( It.IsAny<IMqttChannel<byte[]>>() ) == packetChannel.Object );

            packetChannel
                .Setup( c => c.IsConnected )
                .Returns( true );
            packetChannel
                .Setup( c => c.SenderStream )
                .Returns( new Subject<IPacket>() );
            packetChannel
                .Setup( c => c.ReceiverStream )
                .Returns( packets );

            IProtocolFlowProvider flowProvider = Mock.Of<IProtocolFlowProvider>();
            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();

            MqttServerImpl server = new MqttServerImpl( channelProvider.Object, factory, flowProvider, connectionProvider.Object, Mock.Of<ISubject<MqttUndeliveredMessage>>(), configuration );

            sockets.OnNext( Mock.Of<IMqttChannel<byte[]>>( x => x.ReceiverStream == new Subject<byte[]>() ) );
            sockets.OnNext( Mock.Of<IMqttChannel<byte[]>>( x => x.ReceiverStream == new Subject<byte[]>() ) );
            sockets.OnNext( Mock.Of<IMqttChannel<byte[]>>( x => x.ReceiverStream == new Subject<byte[]>() ) );

            0.Should().Be( server.ActiveConnections );
        }

        [Test]
        public void when_connection_established_then_active_connections_increases()
        {
            Subject<IMqttChannel<byte[]>> sockets = new Subject<IMqttChannel<byte[]>>();
            Mock<IMqttChannelListener> channelProvider = new Mock<IMqttChannelListener>();

            channelProvider
                .Setup( p => p.GetChannelStream() )
                .Returns( sockets );

            MqttConfiguration configuration = Mock.Of<MqttConfiguration>( c => c.WaitTimeoutSecs == 60 );

            Subject<IPacket> packets = new Subject<IPacket>();
            Mock<IMqttChannel<IPacket>> packetChannel = new Mock<IMqttChannel<IPacket>>();
            IPacketChannelFactory factory = Mock.Of<IPacketChannelFactory>( x => x.Create( It.IsAny<IMqttChannel<byte[]>>() ) == packetChannel.Object );

            packetChannel
                .Setup( c => c.IsConnected )
                .Returns( true );
            packetChannel
                .Setup( c => c.SenderStream )
                .Returns( new Subject<IPacket>() );
            packetChannel
                .Setup( c => c.ReceiverStream )
                .Returns( packets );

            IProtocolFlowProvider flowProvider = Mock.Of<IProtocolFlowProvider>();
            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();

            MqttServerImpl server = new MqttServerImpl( channelProvider.Object, factory, flowProvider, connectionProvider.Object, Mock.Of<ISubject<MqttUndeliveredMessage>>(), configuration );

            server.Start();

            sockets.OnNext( Mock.Of<IMqttChannel<byte[]>>( x => x.ReceiverStream == new Subject<byte[]>() ) );
            sockets.OnNext( Mock.Of<IMqttChannel<byte[]>>( x => x.ReceiverStream == new Subject<byte[]>() ) );
            sockets.OnNext( Mock.Of<IMqttChannel<byte[]>>( x => x.ReceiverStream == new Subject<byte[]>() ) );

            3.Should().Be( server.ActiveConnections );
        }

        [Test]
        public void when_server_closed_then_pending_connection_is_closed()
        {
            Subject<IMqttChannel<byte[]>> sockets = new Subject<IMqttChannel<byte[]>>();
            Mock<IMqttChannelListener> channelProvider = new Mock<IMqttChannelListener>();

            channelProvider
                .Setup( p => p.GetChannelStream() )
                .Returns( sockets );

            Mock<IMqttChannel<IPacket>> packetChannel = new Mock<IMqttChannel<IPacket>>();

            packetChannel
                .Setup( c => c.IsConnected )
                .Returns( true );
            packetChannel
                .Setup( c => c.SenderStream )
                .Returns( new Subject<IPacket>() );
            packetChannel
                .Setup( c => c.ReceiverStream )
                .Returns( new Subject<IPacket>() );

            IProtocolFlowProvider flowProvider = Mock.Of<IProtocolFlowProvider>();
            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();

            MqttConfiguration configuration = Mock.Of<MqttConfiguration>( c => c.WaitTimeoutSecs == 60 );

            MqttServerImpl server = new MqttServerImpl( channelProvider.Object, Mock.Of<IPacketChannelFactory>( x => x.Create( It.IsAny<IMqttChannel<byte[]>>() ) == packetChannel.Object ),
                flowProvider, connectionProvider.Object, Mock.Of<ISubject<MqttUndeliveredMessage>>(), configuration );

            server.Start();

            Mock<IMqttChannel<byte[]>> socket = new Mock<IMqttChannel<byte[]>>();

            sockets.OnNext( socket.Object );

            server.Stop();

            packetChannel.Verify( x => x.Dispose() );
        }

        [Test]
        public async Task when_receiver_error_then_closes_connection()
        {
            Subject<IMqttChannel<byte[]>> sockets = new Subject<IMqttChannel<byte[]>>();
            Mock<IMqttChannelListener> channelProvider = new Mock<IMqttChannelListener>();

            channelProvider
                .Setup( p => p.GetChannelStream() )
                .Returns( sockets );

            MqttConfiguration configuration = Mock.Of<MqttConfiguration>( c => c.WaitTimeoutSecs == 60 );

            Subject<IPacket> packets = new Subject<IPacket>();

            Mock<IMqttChannel<IPacket>> packetChannel = new Mock<IMqttChannel<IPacket>>();
            Mock<IPacketChannelFactory> factory = new Mock<IPacketChannelFactory>();

            packetChannel
                .Setup( c => c.IsConnected )
                .Returns( true );
            packetChannel
                .Setup( c => c.SenderStream )
                .Returns( new Subject<IPacket>() );
            packetChannel
                .Setup( c => c.ReceiverStream )
                .Returns( packets );

            factory.Setup( x => x.Create( It.IsAny<IMqttChannel<byte[]>>() ) )
                .Returns( packetChannel.Object );

            IProtocolFlowProvider flowProvider = Mock.Of<IProtocolFlowProvider>();
            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();

            MqttServerImpl server = new MqttServerImpl( channelProvider.Object, factory.Object, flowProvider, connectionProvider.Object, Mock.Of<ISubject<MqttUndeliveredMessage>>(), configuration );
            Subject<byte[]> receiver = new Subject<byte[]>();
            Mock<IMqttChannel<byte[]>> socket = new Mock<IMqttChannel<byte[]>>();

            socket.Setup( x => x.ReceiverStream ).Returns( receiver );

            server.Start();

            sockets.OnNext( socket.Object );

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
