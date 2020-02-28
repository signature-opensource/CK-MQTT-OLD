using CK.MQTT;
using CK.MQTT.Sdk.Bindings;
using NUnit.Framework;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Tests
{
    public class TcpChannelFactorySpec
    {
        [Test]
        public async Task when_creating_channel_then_succeeds()
        {
            MqttConfiguration configuration = new MqttConfiguration { ConnectionTimeoutSecs = 2 };
            TcpListener listener = new TcpListener( IPAddress.Loopback, configuration.Port );

            listener.Start();

            TcpChannelClientFactory factory = new TcpChannelClientFactory( IPAddress.Loopback.ToString(), configuration );
            var channel = await factory.CreateAsync();

            Assert.NotNull( channel );
            Assert.True( channel.Connected );

            listener.Stop();
        }

        [Test]
        public void when_creating_channel_with_invalid_address_then_fails()
        {
            MqttConfiguration configuration = new MqttConfiguration { ConnectionTimeoutSecs = 2 };
            TcpChannelClientFactory factory = new TcpChannelClientFactory( IPAddress.Loopback.ToString(), configuration );
            AggregateException ex = Assert.Throws<AggregateException>( () =>
           {
               var a = factory.CreateAsync().Result;//Why this variable must exist ????
            } );

            Assert.NotNull( ex );
            Assert.NotNull( ex.InnerException );
            Assert.True( ex.InnerException is MqttException );
            Assert.NotNull( ex.InnerException.InnerException );
            Assert.True( ex.InnerException.InnerException is SocketException );
        }
    }
}
