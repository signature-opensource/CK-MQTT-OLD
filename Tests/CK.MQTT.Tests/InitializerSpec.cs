using System;
using System.Net;
using CK.MQTT;
using CK.MQTT.Sdk;
using CK.MQTT.Sdk.Bindings;
using System.Net.Sockets;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace Tests
{
	public class InitializerSpec
	{
		[Test]
		public void when_creating_protocol_configuration_then_default_values_are_set()
		{
			var configuration = new MqttConfiguration ();

			MqttQualityOfService.AtMostOnce.Should().Be(configuration.MaximumQualityOfService);
			0.Should().Be(configuration.KeepAliveSecs);
			5.Should().Be(configuration.WaitTimeoutSecs);
			true.Should().Be(configuration.AllowWildcardsInTopicFilters);
		}

		[Test]
		public void when_initializing_server_then_succeeds()
		{
			var configuration = new MqttConfiguration();
			var binding = new ServerTcpBinding ();
			var initializer = new MqttServerFactory (binding);
			var server = initializer.CreateServer (configuration);

			Assert.NotNull (server);

			server.Stop ();
		}

		[Test]
		public async Task when_initializing_client_then_succeeds()
		{
			var port = new Random().Next(IPEndPoint.MinPort, IPEndPoint.MaxPort);
			var listener = new TcpListener(IPAddress.Loopback, port);

			listener.Start ();

			var binding = new TcpBinding();
			var initializer = new MqttClientFactory ($"{IPAddress.Loopback.ToString()}:{port}({131072})", binding);
			var client = await initializer.CreateClientAsync (new MqttConfiguration());

			Assert.NotNull (client);

			listener.Stop ();
		}
	}
}
