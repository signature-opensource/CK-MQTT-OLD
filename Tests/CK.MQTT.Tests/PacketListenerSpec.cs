using CK.Core;
using CK.MQTT;

using CK.MQTT.Sdk;
using CK.MQTT.Sdk.Flows;
using CK.MQTT.Sdk.Packets;
using CK.MQTT.Sdk.Storage;
using Moq;
using NUnit.Framework;
using System;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

using static CK.Testing.MonitorTestHelper;

namespace Tests
{
    public class PacketListenerSpec
    {
        [Test]
        public void when_packet_is_received_then_it_is_dispatched_to_proper_flow()
        {
            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();
            Mock<IProtocolFlowProvider> flowProvider = new Mock<IProtocolFlowProvider>();
            IRepositoryProvider repositoryProvider = Mock.Of<IRepositoryProvider>();

            Mock<IProtocolFlow> flow = new Mock<IProtocolFlow>();

            flowProvider.Setup( p => p.GetFlow( It.IsAny<MqttPacketType>() ) ).Returns( flow.Object );

            MqttConfiguration configuration = new MqttConfiguration { WaitTimeoutSecs = 10 };
            Subject<Mon<IPacket>> receiver = new Subject<Mon<IPacket>>();
            Mock<IMqttChannel<IPacket>> packetChannel = new Mock<IMqttChannel<IPacket>>();

            packetChannel.Setup( c => c.ReceiverStream ).Returns( receiver );
            packetChannel.Setup( c => c.SenderStream ).Returns( new Subject<Mon<IPacket>>() );

            ServerPacketListener listener = new ServerPacketListener( TestHelper.Monitor, packetChannel.Object, connectionProvider.Object, flowProvider.Object, configuration );

            listener.Listen();

            string clientId = Guid.NewGuid().ToString();
            Connect connect = new Connect( clientId, cleanSession: true, MqttProtocol.SupportedLevel );
            Publish publish = new Publish( Guid.NewGuid().ToString(), MqttQualityOfService.AtMostOnce, false, false );

            receiver.OnNext( new Mon<IPacket>( TestHelper.Monitor, connect ) );
            receiver.OnNext( new Mon<IPacket>( TestHelper.Monitor, publish ) );

            bool connectReceived = false;
            bool publishReceived = false;
            ManualResetEventSlim signal = new ManualResetEventSlim();

            listener.PacketStream.Subscribe( p =>
            {
                if( p.Item is Connect )
                {
                    connectReceived = true;
                }
                else if( p.Item is Publish )
                {
                    publishReceived = true;
                }

                if( connectReceived && publishReceived )
                {
                    signal.Set();
                }
            } );

            bool signalSet = signal.Wait( 2000 );

            Assert.True( signalSet );

            flowProvider.Verify( p => p.GetFlow( It.Is<MqttPacketType>( t => t == MqttPacketType.Publish ) ) );
            flow.Verify( f => f.ExecuteAsync( TestHelper.Monitor, It.Is<string>( s => s == clientId ), It.Is<IPacket>( p => p is Connect ), It.Is<IMqttChannel<IPacket>>( c => c == packetChannel.Object ) ) );
            flow.Verify( f => f.ExecuteAsync( TestHelper.Monitor, It.Is<string>( s => s == clientId ), It.Is<IPacket>( p => p is Publish ), It.Is<IMqttChannel<IPacket>>( c => c == packetChannel.Object ) ) );
        }

        [Test]
        public void when_connect_received_then_client_id_is_added()
        {
            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();
            IProtocolFlowProvider flowProvider = Mock.Of<IProtocolFlowProvider>();
            IRepositoryProvider repositoryProvider = Mock.Of<IRepositoryProvider>();
            MqttConfiguration configuration = new MqttConfiguration { WaitTimeoutSecs = 10 };
            Subject<Mon<IPacket>> receiver = new Subject<Mon<IPacket>>();
            Mock<IMqttChannel<IPacket>> packetChannel = new Mock<IMqttChannel<IPacket>>();

            packetChannel.Setup( c => c.ReceiverStream ).Returns( receiver );
            packetChannel.Setup( c => c.SenderStream ).Returns( new Subject<Mon<IPacket>>() );

            ServerPacketListener listener = new ServerPacketListener( TestHelper.Monitor, packetChannel.Object, connectionProvider.Object, flowProvider, configuration );

            listener.Listen();

            string clientId = Guid.NewGuid().ToString();
            Connect connect = new Connect( clientId, cleanSession: true, MqttProtocol.SupportedLevel );

            receiver.OnNext( new Mon<IPacket>( TestHelper.Monitor, connect ) );

            connectionProvider.Verify( m => m.AddConnection( TestHelper.Monitor, It.Is<string>( s => s == clientId ), It.Is<IMqttChannel<IPacket>>( c => c == packetChannel.Object ) ) );
        }

        [Test]
        public void when_no_connect_is_received_then_times_out()
        {
            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();
            IProtocolFlowProvider flowProvider = Mock.Of<IProtocolFlowProvider>();
            IRepositoryProvider repositoryProvider = Mock.Of<IRepositoryProvider>();
            int waitingTimeout = 1;
            MqttConfiguration configuration = new MqttConfiguration { WaitTimeoutSecs = waitingTimeout };
            Subject<Mon<IPacket>> receiver = new Subject<Mon<IPacket>>();
            Mock<IMqttChannel<IPacket>> packetChannel = new Mock<IMqttChannel<IPacket>>();

            packetChannel.Setup( c => c.ReceiverStream ).Returns( receiver );
            packetChannel.Setup( c => c.SenderStream ).Returns( new Subject<Mon<IPacket>>() );

            ServerPacketListener listener = new ServerPacketListener( TestHelper.Monitor, packetChannel.Object, connectionProvider.Object, flowProvider, configuration );

            listener.Listen();

            ManualResetEventSlim timeoutSignal = new ManualResetEventSlim( initialState: false );

            listener.PacketStream.Subscribe( _ => { }, ex =>
            {
                timeoutSignal.Set();
            } );

            bool timeoutOccurred = timeoutSignal.Wait( (waitingTimeout + 1) * 1000 );

            Assert.True( timeoutOccurred );
        }

        [Test]
        public void when_connect_is_received_then_does_not_time_out()
        {
            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();
            IProtocolFlowProvider flowProvider = Mock.Of<IProtocolFlowProvider>();
            IRepositoryProvider repositoryProvider = Mock.Of<IRepositoryProvider>();
            int waitingTimeout = 1;
            MqttConfiguration configuration = new MqttConfiguration { WaitTimeoutSecs = waitingTimeout };
            Subject<Mon<IPacket>> receiver = new Subject<Mon<IPacket>>();
            Mock<IMqttChannel<IPacket>> packetChannel = new Mock<IMqttChannel<IPacket>>();

            packetChannel.Setup( c => c.ReceiverStream ).Returns( receiver );
            packetChannel.Setup( c => c.SenderStream ).Returns( new Subject<Mon<IPacket>>() );

            ServerPacketListener listener = new ServerPacketListener( TestHelper.Monitor, packetChannel.Object, connectionProvider.Object, flowProvider, configuration );

            listener.Listen();

            bool timeoutOccured = false;

            listener.PacketStream.Subscribe( _ => { }, ex =>
            {
                timeoutOccured = true;
            } );

            string clientId = Guid.NewGuid().ToString();
            Connect connect = new Connect( clientId, cleanSession: true, MqttProtocol.SupportedLevel );

            receiver.OnNext( new Mon<IPacket>( TestHelper.Monitor, connect ) );

            Assert.False( timeoutOccured );
        }

        [Test]
        public void when_first_packet_is_not_connect_then_fails()
        {
            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();
            IProtocolFlowProvider flowProvider = Mock.Of<IProtocolFlowProvider>();
            IRepositoryProvider repositoryProvider = Mock.Of<IRepositoryProvider>();
            MqttConfiguration configuration = new MqttConfiguration { WaitTimeoutSecs = 10 };
            Subject<Mon<IPacket>> receiver = new Subject<Mon<IPacket>>();
            Mock<IMqttChannel<IPacket>> packetChannel = new Mock<IMqttChannel<IPacket>>();

            packetChannel.Setup( c => c.ReceiverStream ).Returns( receiver );
            packetChannel.Setup( c => c.SenderStream ).Returns( new Subject<Mon<IPacket>>() );

            ServerPacketListener listener = new ServerPacketListener( TestHelper.Monitor, packetChannel.Object, connectionProvider.Object, flowProvider, configuration );

            listener.Listen();

            bool errorOccured = false;

            listener.PacketStream.Subscribe( _ => { }, ex =>
            {
                errorOccured = true;
            } );

            receiver.OnNext( new Mon<IPacket>( TestHelper.Monitor, new PingRequest() ) );

            Assert.True( errorOccured );
        }

        [Test]
        public void when_second_connect_received_then_fails()
        {
            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();

            connectionProvider.Setup( p => p.RemoveConnection( TestHelper.Monitor, It.IsAny<string>() ) );

            Mock<IServerPublishReceiverFlow> serverPublishReceiverFlow = new Mock<IServerPublishReceiverFlow>();
            Mock<IProtocolFlowProvider> flowProvider = new Mock<IProtocolFlowProvider>();
            MqttConfiguration configuration = new MqttConfiguration { WaitTimeoutSecs = 10 };
            Subject<Mon<IPacket>> receiver = new Subject<Mon<IPacket>>();
            Mock<IMqttChannel<IPacket>> packetChannel = new Mock<IMqttChannel<IPacket>>();

            serverPublishReceiverFlow.Setup( f => f.SendWillAsync( TestHelper.Monitor, It.IsAny<string>() ) ).Returns( Task.FromResult( true ) );
            flowProvider.Setup( p => p.GetFlow<IServerPublishReceiverFlow>() ).Returns( serverPublishReceiverFlow.Object );
            packetChannel.Setup( c => c.ReceiverStream ).Returns( receiver );
            packetChannel.Setup( c => c.SenderStream ).Returns( new Subject<Mon<IPacket>>() );

            ServerPacketListener listener = new ServerPacketListener( TestHelper.Monitor, packetChannel.Object, connectionProvider.Object, flowProvider.Object, configuration );

            listener.Listen();

            ManualResetEventSlim errorSignal = new ManualResetEventSlim();

            listener.PacketStream.Subscribe( _ => { }, ex =>
            {
                errorSignal.Set();
            } );

            string clientId = Guid.NewGuid().ToString();
            Connect connect = new Connect( clientId, cleanSession: true, MqttProtocol.SupportedLevel );

            receiver.OnNext( new Mon<IPacket>( TestHelper.Monitor, connect ) );
            receiver.OnNext( new Mon<IPacket>( TestHelper.Monitor, connect ) );

            bool errorOccured = errorSignal.Wait( TimeSpan.FromSeconds( 1 ) );

            Assert.True( errorOccured );
            connectionProvider.Verify( p => p.RemoveConnection( TestHelper.Monitor, It.Is<string>( s => s == clientId ) ) );
        }

        [Test]
        public void when_keep_alive_enabled_and_no_packet_received_then_times_out()
        {
            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();
            Mock<IServerPublishReceiverFlow> serverPublishReceiverFlow = new Mock<IServerPublishReceiverFlow>();
            Mock<IProtocolFlowProvider> flowProvider = new Mock<IProtocolFlowProvider>();

            serverPublishReceiverFlow.Setup( f => f.SendWillAsync( TestHelper.Monitor, It.IsAny<string>() ) )
                .Returns( Task.FromResult( true ) );
            flowProvider.Setup( p => p.GetFlow<IServerPublishReceiverFlow>() )
                .Returns( serverPublishReceiverFlow.Object );
            flowProvider.Setup( p => p.GetFlow( It.IsAny<MqttPacketType>() ) )
                .Returns( Mock.Of<IProtocolFlow>() );

            MqttConfiguration configuration = new MqttConfiguration { WaitTimeoutSecs = 10 };
            Subject<Mon<IPacket>> receiver = new Subject<Mon<IPacket>>();
            Subject<Mon<IPacket>> sender = new Subject<Mon<IPacket>>();
            Mock<IMqttChannel<IPacket>> packetChannelMock = new Mock<IMqttChannel<IPacket>>();

            packetChannelMock.Setup( c => c.ReceiverStream ).Returns( receiver );
            packetChannelMock.Setup( c => c.SenderStream ).Returns( sender );

            IMqttChannel<IPacket> packetChannel = packetChannelMock.Object;

            ServerPacketListener listener = new ServerPacketListener( TestHelper.Monitor, packetChannel, connectionProvider.Object, flowProvider.Object, configuration );

            listener.Listen();

            ManualResetEventSlim timeoutSignal = new ManualResetEventSlim( initialState: false );

            listener.PacketStream.Subscribe( _ => { }, ex =>
            {
                timeoutSignal.Set();
            }, () =>
            {
                timeoutSignal.Set();
            } );

            string clientId = Guid.NewGuid().ToString();
            ushort keepAlive = 1;
            Connect connect = new Connect( clientId, cleanSession: true, MqttProtocol.SupportedLevel ) { KeepAlive = keepAlive };

            receiver.OnNext( new Mon<IPacket>( TestHelper.Monitor, connect ) );

            ConnectAck connectAck = new ConnectAck( MqttConnectionStatus.Accepted, existingSession: false );

            sender.OnNext( new Mon<IPacket>( TestHelper.Monitor, connectAck ) );

            bool timeoutOccurred = timeoutSignal.Wait( ((int)((keepAlive + 1) * 1.5) * 1000) );

            Assert.True( timeoutOccurred );
        }

        [Test]
        public void when_keep_alive_enabled_and_packet_received_then_does_not_time_out()
        {
            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();
            Mock<IProtocolFlowProvider> flowProvider = new Mock<IProtocolFlowProvider>();

            flowProvider.Setup( p => p.GetFlow( It.IsAny<MqttPacketType>() ) )
                .Returns( Mock.Of<IProtocolFlow>() );

            IRepositoryProvider repositoryProvider = Mock.Of<IRepositoryProvider>();
            MqttConfiguration configuration = new MqttConfiguration { WaitTimeoutSecs = 10 };
            Subject<Mon<IPacket>> receiver = new Subject<Mon<IPacket>>();
            Mock<IMqttChannel<IPacket>> packetChannel = new Mock<IMqttChannel<IPacket>>();

            packetChannel.Setup( c => c.ReceiverStream ).Returns( receiver );
            packetChannel.Setup( c => c.SenderStream ).Returns( new Subject<Mon<IPacket>>() );

            ServerPacketListener listener = new ServerPacketListener( TestHelper.Monitor, packetChannel.Object, connectionProvider.Object, flowProvider.Object, configuration );

            listener.Listen();

            bool timeoutOccured = false;

            listener.PacketStream.Subscribe( _ => { }, ex =>
            {
                timeoutOccured = true;
            } );

            string clientId = Guid.NewGuid().ToString();
            ushort keepAlive = 1;
            Connect connect = new Connect( clientId, cleanSession: true, MqttProtocol.SupportedLevel ) { KeepAlive = keepAlive };

            receiver.OnNext( new Mon<IPacket>( TestHelper.Monitor, connect ) );
            packetChannel.Object.SendAsync( new Mon<IPacket>( TestHelper.Monitor, new ConnectAck( MqttConnectionStatus.Accepted, existingSession: false ) ) ).Wait();
            receiver.OnNext( new Mon<IPacket>( TestHelper.Monitor, new PingRequest() ) );

            Assert.False( timeoutOccured );
        }

        [Test]
        public void when_keep_alive_disabled_and_no_packet_received_then_does_not_time_out()
        {
            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();
            Mock<IProtocolFlowProvider> flowProvider = new Mock<IProtocolFlowProvider>();

            flowProvider.Setup( p => p.GetFlow( It.IsAny<MqttPacketType>() ) )
                .Returns( Mock.Of<IProtocolFlow>() );

            IRepositoryProvider repositoryProvider = Mock.Of<IRepositoryProvider>();
            MqttConfiguration configuration = new MqttConfiguration { WaitTimeoutSecs = 10 };
            Subject<Mon<IPacket>> receiver = new Subject<Mon<IPacket>>();
            Mock<IMqttChannel<IPacket>> packetChannel = new Mock<IMqttChannel<IPacket>>();

            packetChannel.Setup( c => c.ReceiverStream ).Returns( receiver );
            packetChannel.Setup( c => c.SenderStream ).Returns( new Subject<Mon<IPacket>>() );

            ServerPacketListener listener = new ServerPacketListener( TestHelper.Monitor, packetChannel.Object, connectionProvider.Object, flowProvider.Object, configuration );

            listener.Listen();

            ManualResetEventSlim timeoutSignal = new ManualResetEventSlim( initialState: false );

            listener.PacketStream.Subscribe( _ => { }, ex =>
            {
                timeoutSignal.Set();
            } );

            string clientId = Guid.NewGuid().ToString();
            Connect connect = new Connect( clientId, cleanSession: true, MqttProtocol.SupportedLevel ) { KeepAlive = 0 };

            receiver.OnNext( new Mon<IPacket>( TestHelper.Monitor, connect ) );
            packetChannel.Object.SendAsync( new Mon<IPacket>( TestHelper.Monitor, new ConnectAck( MqttConnectionStatus.Accepted, existingSession: false ) ) ).Wait();

            bool timeoutOccurred = timeoutSignal.Wait( 2000 );

            Assert.False( timeoutOccurred );
        }
    }
}
