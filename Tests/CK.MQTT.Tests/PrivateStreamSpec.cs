using System;
using CK.MQTT;
using CK.MQTT.Sdk.Bindings;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace Tests
{
    public class PrivateStreamSpec
    {
        [Test]
        public void when_creating_stream_then_becomes_ready ()
        {
            var configuration = new MqttConfiguration ();
            var stream = new PrivateStream (configuration);

            Assert.True (!stream.IsDisposed);
        }

        [Test]
        public async Task when_sending_payload_with_identifier_then_receives_on_same_identifier ()
        {
            var configuration = new MqttConfiguration ();
            var stream = new PrivateStream (configuration);

            var clientReceived = 0;
            var clientReceiver = stream
                .Receive (EndpointIdentifier.Client)
                .Subscribe (payload => {
                    clientReceived++;
                });

            var serverReceived = 0;
            var serverReceiver = stream
                .Receive (EndpointIdentifier.Server)
                .Subscribe (payload => {
                    serverReceived++;
                });

            stream.Send (new byte[255], EndpointIdentifier.Client);
            stream.Send (new byte[100], EndpointIdentifier.Client);
            stream.Send (new byte[30], EndpointIdentifier.Client);
            stream.Send (new byte[10], EndpointIdentifier.Server);
            stream.Send (new byte[500], EndpointIdentifier.Server);
            stream.Send (new byte[5], EndpointIdentifier.Server);

            await Task.Delay (TimeSpan.FromMilliseconds (1000));

            3.Should().Be(clientReceived);
            3.Should().Be(serverReceived);
        }

        [Test]
        public async Task when_sending_payload_with_identifier_then_does_not_receive_on_other_identifier()
        {
            var configuration = new MqttConfiguration ();
            var stream = new PrivateStream (configuration);

            var serverReceived = 0;
            var serverReceiver = stream
                .Receive (EndpointIdentifier.Server)
                .Subscribe (payload => {
                    serverReceived++;
                });

            stream.Send (new byte[255], EndpointIdentifier.Client);
            stream.Send (new byte[100], EndpointIdentifier.Client);
            stream.Send (new byte[30], EndpointIdentifier.Client);

            await Task.Delay (TimeSpan.FromMilliseconds(1000));

            0.Should().Be(serverReceived);
        }

        [Test]
        public void when_disposing_stream_then_becomes_not_ready ()
        {
            var configuration = new MqttConfiguration();
            var stream = new PrivateStream (configuration);

            stream.Dispose ();

            Assert.True(stream.IsDisposed);
        }
    }
}
