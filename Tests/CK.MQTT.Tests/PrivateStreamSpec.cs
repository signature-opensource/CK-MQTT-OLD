using CK.MQTT;
using CK.MQTT.Client.Abstractions;
using CK.MQTT.Sdk.Bindings;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

using static CK.Testing.MonitorTestHelper;

namespace Tests
{
    public class PrivateStreamSpec
    {
        [Test]
        public void when_creating_stream_then_becomes_ready()
        {
            MqttConfiguration configuration = new MqttConfiguration();
            PrivateStream stream = new PrivateStream( configuration );

            Assert.True( !stream.IsDisposed );
        }

        [Test]
        public async Task when_sending_payload_with_identifier_then_receives_on_same_identifier()
        {
            MqttConfiguration configuration = new MqttConfiguration();
            PrivateStream stream = new PrivateStream( configuration );

            int clientReceived = 0;
            IDisposable clientReceiver = stream
                .Receive( EndpointIdentifier.Client )
                .Subscribe( payload =>
                {
                    clientReceived++;
                } );

            int serverReceived = 0;
            IDisposable serverReceiver = stream
                .Receive( EndpointIdentifier.Server )
                .Subscribe( payload =>
                {
                    serverReceived++;
                } );

            stream.Send( new Monitored<byte[]>( TestHelper.Monitor, new byte[255] ), EndpointIdentifier.Client );
            stream.Send( new Monitored<byte[]>( TestHelper.Monitor, new byte[100] ), EndpointIdentifier.Client );
            stream.Send( new Monitored<byte[]>( TestHelper.Monitor, new byte[30] ), EndpointIdentifier.Client );
            stream.Send( new Monitored<byte[]>( TestHelper.Monitor, new byte[10] ), EndpointIdentifier.Server );
            stream.Send( new Monitored<byte[]>( TestHelper.Monitor, new byte[500] ), EndpointIdentifier.Server );
            stream.Send( new Monitored<byte[]>( TestHelper.Monitor, new byte[5] ), EndpointIdentifier.Server );

            await Task.Delay( TimeSpan.FromMilliseconds( 1000 ) );

            3.Should().Be( clientReceived );
            3.Should().Be( serverReceived );
        }

        [Test]
        public async Task when_sending_payload_with_identifier_then_does_not_receive_on_other_identifier()
        {
            MqttConfiguration configuration = new MqttConfiguration();
            PrivateStream stream = new PrivateStream( configuration );

            int serverReceived = 0;
            IDisposable serverReceiver = stream
                .Receive( EndpointIdentifier.Server )
                .Subscribe( payload =>
                {
                    serverReceived++;
                } );

            stream.Send( new Monitored<byte[]>( TestHelper.Monitor, new byte[255] ), EndpointIdentifier.Client );
            stream.Send( new Monitored<byte[]>( TestHelper.Monitor, new byte[100] ), EndpointIdentifier.Client );
            stream.Send( new Monitored<byte[]>( TestHelper.Monitor, new byte[30] ), EndpointIdentifier.Client );

            await Task.Delay( TimeSpan.FromMilliseconds( 1000 ) );

            0.Should().Be( serverReceived );
        }

        [Test]
        public void when_disposing_stream_then_becomes_not_ready()
        {
            MqttConfiguration configuration = new MqttConfiguration();
            PrivateStream stream = new PrivateStream( configuration );

            stream.Dispose();

            Assert.True( stream.IsDisposed );
        }
    }
}
