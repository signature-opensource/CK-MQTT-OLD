using System;
using CK.MQTT;
using CK.MQTT.Sdk.Bindings;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace Tests
{
    public class PrivateChannelProviderSpec
    {
        [Test]
        public async Task when_gettting_channels_with_stream_then_succeeds ()
        {
            var configuration = new MqttConfiguration ();
            var privateStreamListener = new Subject<PrivateStream> ();
            var provider = new PrivateChannelListener (privateStreamListener, configuration);

            var channelsCreated = 0;

            provider
                .GetChannelStream ()
                .Subscribe (channel => {
                    channelsCreated++;
                });

            privateStreamListener.OnNext (new PrivateStream (configuration));
            privateStreamListener.OnNext (new PrivateStream (configuration));
            privateStreamListener.OnNext (new PrivateStream (configuration));

            await Task.Delay (TimeSpan.FromMilliseconds(1000));

            3.Should().Be(channelsCreated);
        }

        [Test]
        public async Task when_gettting_channels_without_stream_then_fails ()
        {
            var configuration = new MqttConfiguration ();
            var privateStreamListener = new Subject<PrivateStream> ();
            var provider = new PrivateChannelListener (privateStreamListener, configuration);

            var channelsCreated = 0;

            provider
                .GetChannelStream ()
                .Subscribe (channel => {
                    channelsCreated++;
                });

            await Task.Delay (TimeSpan.FromMilliseconds(1000));

            0.Should().Be(channelsCreated);
        }
    }
}
