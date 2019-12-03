using System;
using System.Net;
using CK.MQTT;
using CK.MQTT.Sdk.Bindings;
using System.Net.Sockets;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Tests
{
	public class TcpChannelFactorySpec
	{
		[Test]
		public async Task when_creating_channel_then_succeeds()
		{
			var configuration = new MqttConfiguration { ConnectionTimeoutSecs = 2 };
			var listener = new TcpListener (IPAddress.Loopback, configuration.Port);

			listener.Start ();

			var factory = new TcpChannelFactory (IPAddress.Loopback.ToString (), configuration);
			var channel = await factory.CreateAsync ();

			Assert.NotNull (channel);
			Assert.True (channel.IsConnected);

			listener.Stop ();
		}

		[Test]
		public void when_creating_channel_with_invalid_address_then_fails()
		{
			var configuration = new MqttConfiguration { ConnectionTimeoutSecs = 2 };
			var factory = new TcpChannelFactory (IPAddress.Loopback.ToString (), configuration);
			var ex = Assert.Throws<AggregateException> ( () =>
            {
                var a = factory.CreateAsync().Result;//Why this variable must exist ????
            } );

			Assert.NotNull (ex);
			Assert.NotNull (ex.InnerException);
			Assert.True (ex.InnerException is MqttException);
			Assert.NotNull (ex.InnerException.InnerException);
			Assert.True (ex.InnerException.InnerException is SocketException);
		}
	}
}
