using CK.MQTT;
using CK.MQTT.Sdk;
using CK.MQTT.Sdk.Bindings;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Tests
{
    public class InitializerSpec
    {
        [Test]
        public void when_creating_protocol_configuration_then_default_values_are_set()
        {
            MqttConfiguration configuration = new MqttConfiguration();

            MqttProtocol.DefaultNonSecurePort.Should().Be( configuration.Port );
            8192.Should().Be( configuration.BufferSize );
            MqttQualityOfService.AtMostOnce.Should().Be( configuration.MaximumQualityOfService );
            0.Should().Be( configuration.KeepAliveSecs );
            5.Should().Be( configuration.WaitTimeoutSecs );
            true.Should().Be( configuration.AllowWildcardsInTopicFilters );
        }

        [Test]
        public void when_initializing_server_then_succeeds()
        {
            MqttConfiguration configuration = new MqttConfiguration
            {
                BufferSize = 131072,
                Port = MqttProtocol.DefaultNonSecurePort
            };
            ServerTcpBinding binding = new ServerTcpBinding();
            MqttServerFactory initializer = new MqttServerFactory( binding );
            IMqttServer server = initializer.CreateServer( configuration );

            Assert.NotNull( server );

            server.Stop();
        }

        [Test]
        public async Task when_initializing_client_then_succeeds()
        {
            int port = new Random().Next( IPEndPoint.MinPort, IPEndPoint.MaxPort );
            TcpListener listener = new TcpListener( IPAddress.Loopback, port );

            listener.Start();

            MqttConfiguration configuration = new MqttConfiguration
            {
                BufferSize = 131072,
                Port = port
            };
            TcpBinding binding = new TcpBinding();
            MqttClientFactory initializer = new MqttClientFactory( IPAddress.Loopback.ToString(), binding );
            IMqttClient client = await initializer.CreateClientAsync( configuration );

            Assert.NotNull( client );

            listener.Stop();
        }
    }
}
