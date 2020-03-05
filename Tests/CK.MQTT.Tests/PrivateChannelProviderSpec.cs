using CK.Core;
using CK.MQTT;
using CK.MQTT.Sdk.Bindings;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Reactive.Subjects;
using System.Threading.Tasks;

using static CK.Testing.MonitorTestHelper;

namespace Tests
{
    public class PrivateChannelProviderSpec
    {
        [Test]
        public async Task when_gettting_channels_with_stream_then_succeeds()
        {
            MqttConfiguration configuration = new MqttConfiguration();
            Subject<Mon<PrivateStream>> privateStreamListener = new Subject<Mon<PrivateStream>>();
            PrivateChannelListener provider = new PrivateChannelListener( privateStreamListener, configuration );

            int channelsCreated = 0;

            provider
                .GetChannelStream()
                .Subscribe( channel =>
                {
                    channelsCreated++;
                } );

            privateStreamListener.OnNext( new Mon<PrivateStream>( TestHelper.Monitor, new PrivateStream( configuration ) ) );
            privateStreamListener.OnNext( new Mon<PrivateStream>( TestHelper.Monitor, new PrivateStream( configuration ) ) );
            privateStreamListener.OnNext( new Mon<PrivateStream>( TestHelper.Monitor, new PrivateStream( configuration ) ) );

            await Task.Delay( TimeSpan.FromMilliseconds( 1000 ) );

            3.Should().Be( channelsCreated );
        }

        [Test]
        public async Task when_gettting_channels_without_stream_then_fails()
        {
            MqttConfiguration configuration = new MqttConfiguration();
            Subject<Mon<PrivateStream>> privateStreamListener = new Subject<Mon<PrivateStream>>();
            PrivateChannelListener provider = new PrivateChannelListener( privateStreamListener, configuration );

            int channelsCreated = 0;

            provider
                .GetChannelStream()
                .Subscribe( channel =>
                {
                    channelsCreated++;
                } );

            await Task.Delay( TimeSpan.FromMilliseconds( 1000 ) );

            0.Should().Be( channelsCreated );
        }
    }
}
