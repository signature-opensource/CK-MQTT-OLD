using CK.Core;
using CK.MQTT;

using CK.MQTT.Sdk.Bindings;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

using static CK.Testing.MonitorTestHelper;

namespace Tests
{
    public class PrivateChannelSpec
    {
        [Test]
        public void when_creating_channel_with_stream_ready_then_it_is_connected()
        {
            MqttConfiguration configuration = new MqttConfiguration();
            PrivateStream stream = new PrivateStream( configuration );
            PrivateChannel channel = new PrivateChannel( TestHelper.Monitor, stream, EndpointIdentifier.Client, configuration );

            Assert.True( channel.IsConnected );
            Assert.NotNull( channel.ReceiverStream );
            Assert.NotNull( channel.SenderStream );
        }

        [Test]
        public void when_creating_channel_with_stream_disposed_then_fails()
        {
            MqttConfiguration configuration = new MqttConfiguration();
            PrivateStream stream = new PrivateStream( configuration );

            stream.Dispose();

            ObjectDisposedException ex = Assert.Throws<ObjectDisposedException>( () => new PrivateChannel( TestHelper.Monitor, stream, EndpointIdentifier.Client, configuration ) );

            Assert.NotNull( ex );
        }

        [Test]
        public async Task when_sending_packet_then_stream_receives_successfully()
        {
            MqttConfiguration configuration = new MqttConfiguration();
            PrivateStream stream = new PrivateStream( configuration );
            PrivateChannel channel = new PrivateChannel( TestHelper.Monitor, stream, EndpointIdentifier.Client, configuration );

            int packetsReceived = 0;

            stream
                .Receive( EndpointIdentifier.Client )
                .Subscribe( packet =>
                {
                    packetsReceived++;
                } );

            await channel.SendAsync( new Monitored<byte[]>( TestHelper.Monitor, new byte[255] ) );
            await channel.SendAsync( new Monitored<byte[]>( TestHelper.Monitor, new byte[10] ) );
            await channel.SendAsync( new Monitored<byte[]>( TestHelper.Monitor, new byte[34] ) );
            await channel.SendAsync( new Monitored<byte[]>( TestHelper.Monitor, new byte[100] ) );
            await channel.SendAsync( new Monitored<byte[]>( TestHelper.Monitor, new byte[50] ) );

            await Task.Delay( TimeSpan.FromMilliseconds( 1000 ) );

            5.Should().Be( packetsReceived );
        }

        [Test]
        public async Task when_sending_to_stream_then_channel_receives_successfully()
        {
            MqttConfiguration configuration = new MqttConfiguration();
            PrivateStream stream = new PrivateStream( configuration );
            PrivateChannel channel = new PrivateChannel( TestHelper.Monitor, stream, EndpointIdentifier.Server, configuration );

            int packetsReceived = 0;

            channel.ReceiverStream.Subscribe( packet =>
            {
                packetsReceived++;
            } );

            stream.Send( new Monitored<byte[]>( TestHelper.Monitor, new byte[255] ), EndpointIdentifier.Client );
            stream.Send( new Monitored<byte[]>( TestHelper.Monitor, new byte[10] ), EndpointIdentifier.Client );
            stream.Send( new Monitored<byte[]>( TestHelper.Monitor, new byte[34] ), EndpointIdentifier.Client );
            stream.Send( new Monitored<byte[]>( TestHelper.Monitor, new byte[100] ), EndpointIdentifier.Client );
            stream.Send( new Monitored<byte[]>( TestHelper.Monitor, new byte[50] ), EndpointIdentifier.Client );

            await Task.Delay( TimeSpan.FromMilliseconds( 1000 ) );

            5.Should().Be( packetsReceived );
        }

        [Test]
        public void when_disposing_channel_then_became_disconnected()
        {
            MqttConfiguration configuration = new MqttConfiguration();
            PrivateStream stream = new PrivateStream( configuration );
            PrivateChannel channel = new PrivateChannel( TestHelper.Monitor, stream, EndpointIdentifier.Server, configuration );

            channel.Dispose();

            Assert.False( channel.IsConnected );
        }
    }
}
