using Moq;
using System;
using CK.MQTT;
using CK.MQTT.Sdk.Flows;
using CK.MQTT.Sdk.Packets;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using CK.MQTT.Sdk;
using FluentAssertions;
using NUnit.Framework;

namespace Tests
{
    public class ServerSpec
	{
		[Test]
		public void when_server_does_not_start_then_connections_are_ignored ()
		{
			var sockets = new Subject<IMqttChannel<byte[]>> ();
			var channelProvider = new Mock<IMqttChannelListener> ();

			channelProvider
				.Setup (p => p.GetChannelStream ())
				.Returns (sockets);

			var configuration = Mock.Of<MqttConfiguration> (c => c.WaitTimeoutSecs == 60);

			var packets = new Subject<IPacket> ();
			var packetChannel = new Mock<IMqttChannel<IPacket>>();
			var factory = Mock.Of<IPacketChannelFactory> (x => x.Create (It.IsAny<IMqttChannel<byte[]>> ()) == packetChannel.Object);

			packetChannel
				.Setup (c => c.IsConnected)
				.Returns (true);
			packetChannel
				.Setup (c => c.SenderStream)
				.Returns(new Subject<IPacket> ());
			packetChannel
				.Setup (c => c.ReceiverStream)
				.Returns(packets);

			var flowProvider = Mock.Of<IProtocolFlowProvider> ();
			var connectionProvider = new Mock<IConnectionProvider> ();

			var server = new MqttServerImpl (channelProvider.Object, factory, flowProvider, connectionProvider.Object, Mock.Of<ISubject<MqttUndeliveredMessage>>(), configuration);

			sockets.OnNext (Mock.Of<IMqttChannel<byte[]>> (x => x.ReceiverStream == new Subject<byte[]> ()));
			sockets.OnNext (Mock.Of<IMqttChannel<byte[]>> (x => x.ReceiverStream == new Subject<byte[]> ()));
			sockets.OnNext (Mock.Of<IMqttChannel<byte[]>> (x => x.ReceiverStream == new Subject<byte[]> ()));

			0.Should().Be(server.ActiveConnections);
		}

		[Test]
		public void when_connection_established_then_active_connections_increases ()
		{
			var sockets = new Subject<IMqttChannel<byte[]>> ();
			var channelProvider = new Mock<IMqttChannelListener> ();

			channelProvider
				.Setup (p => p.GetChannelStream ())
				.Returns (sockets);

			var configuration = Mock.Of<MqttConfiguration> (c => c.WaitTimeoutSecs == 60);

			var packets = new Subject<IPacket> ();
			var packetChannel = new Mock<IMqttChannel<IPacket>>();
			var factory = Mock.Of<IPacketChannelFactory> (x => x.Create (It.IsAny<IMqttChannel<byte[]>> ()) == packetChannel.Object);

			packetChannel
				.Setup (c => c.IsConnected)
				.Returns (true);
			packetChannel
				.Setup (c => c.SenderStream)
				.Returns(new Subject<IPacket> ());
			packetChannel
				.Setup (c => c.ReceiverStream)
				.Returns(packets);

			var flowProvider = Mock.Of<IProtocolFlowProvider> ();
			var connectionProvider = new Mock<IConnectionProvider> ();

			var server = new MqttServerImpl (channelProvider.Object, factory, flowProvider, connectionProvider.Object, Mock.Of<ISubject<MqttUndeliveredMessage>> (), configuration);

			server.Start ();

			sockets.OnNext (Mock.Of<IMqttChannel<byte[]>> (x => x.ReceiverStream == new Subject<byte[]> ()));
			sockets.OnNext (Mock.Of<IMqttChannel<byte[]>> (x => x.ReceiverStream == new Subject<byte[]> ()));
			sockets.OnNext (Mock.Of<IMqttChannel<byte[]>> (x => x.ReceiverStream == new Subject<byte[]> ()));

			3.Should().Be(server.ActiveConnections);
		}

		[Test]
		public void when_server_closed_then_pending_connection_is_closed ()
		{
			var sockets = new Subject<IMqttChannel<byte[]>> ();
			var channelProvider = new Mock<IMqttChannelListener> ();

			channelProvider
				.Setup (p => p.GetChannelStream ())
				.Returns (sockets);

			var packetChannel = new Mock<IMqttChannel<IPacket>> ();

			packetChannel
				.Setup (c => c.IsConnected)
				.Returns (true);
			packetChannel
				.Setup (c => c.SenderStream)
				.Returns(new Subject<IPacket> ());
			packetChannel
				.Setup (c => c.ReceiverStream)
				.Returns(new Subject<IPacket> ());

			var flowProvider = Mock.Of<IProtocolFlowProvider> ();
			var connectionProvider = new Mock<IConnectionProvider> ();

			var configuration = Mock.Of<MqttConfiguration> (c => c.WaitTimeoutSecs == 60);

			var server = new MqttServerImpl (channelProvider.Object, Mock.Of<IPacketChannelFactory> (x => x.Create (It.IsAny<IMqttChannel<byte[]>> ()) == packetChannel.Object), 
				flowProvider, connectionProvider.Object, Mock.Of<ISubject<MqttUndeliveredMessage>> (), configuration);

			server.Start ();

			var socket = new Mock<IMqttChannel<byte[]>> ();

			sockets.OnNext (socket.Object);

			server.Stop ();

			packetChannel.Verify (x => x.Dispose ());
		}

		[Test]
		public async Task when_receiver_error_then_closes_connection ()
		{
			var sockets = new Subject<IMqttChannel<byte[]>> ();
			var channelProvider = new Mock<IMqttChannelListener> ();

			channelProvider
				.Setup (p => p.GetChannelStream ())
				.Returns (sockets);

			var configuration = Mock.Of<MqttConfiguration> (c => c.WaitTimeoutSecs == 60);

			var packets = new Subject<IPacket> ();

			var packetChannel = new Mock<IMqttChannel<IPacket>> ();
			var factory = new Mock<IPacketChannelFactory> ();

			packetChannel
				.Setup (c => c.IsConnected)
				.Returns (true);
			packetChannel
				.Setup (c => c.SenderStream)
				.Returns(new Subject<IPacket> ());
			packetChannel
				.Setup (c => c.ReceiverStream)
				.Returns(packets);

			factory.Setup (x => x.Create (It.IsAny<IMqttChannel<byte[]>> ()))
				.Returns (packetChannel.Object);

			var flowProvider = Mock.Of<IProtocolFlowProvider> ();
			var connectionProvider = new Mock<IConnectionProvider> ();

			var server = new MqttServerImpl (channelProvider.Object, factory.Object, flowProvider, connectionProvider.Object, Mock.Of<ISubject<MqttUndeliveredMessage>> (), configuration);
			var receiver = new Subject<byte[]> ();
			var socket = new Mock<IMqttChannel<byte[]>> ();

			socket.Setup (x => x.ReceiverStream).Returns (receiver);

			server.Start ();

			sockets.OnNext (socket.Object);

			try {
				packets.OnError (new Exception ("Protocol exception"));
			} catch (Exception) {
			}

            await Task.Delay (TimeSpan.FromMilliseconds (1000));

            packetChannel.Verify (x => x.Dispose ());
		}
	}
}
