using CK.Core;

using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace CK.MQTT.Sdk.Bindings
{
    internal class PrivateChannelListener : IMqttChannelListener
    {
        readonly ISubject<Mon<PrivateStream>> _privateStreamListener;
        readonly MqttConfiguration _configuration;

        public PrivateChannelListener( ISubject<Mon<PrivateStream>> privateStreamListener, MqttConfiguration configuration )
        {
            _privateStreamListener = privateStreamListener;
            _configuration = configuration;
        }

        public IObservable<Mon<IMqttChannel<byte[]>>> ChannelStream =>
            _privateStreamListener
                .Select( stream => new Mon<IMqttChannel<byte[]>>(
                    stream.Monitor,
                    new PrivateChannel( stream.Monitor, stream.Item, EndpointIdentifier.Server, _configuration )
                )
            );

        public void Dispose()
        {
            //Nothing to dispose
        }
    }
}
