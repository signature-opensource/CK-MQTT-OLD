using CK.Core;

using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace CK.MQTT.Sdk.Bindings
{
    internal class PrivateChannelListener : IMqttChannelListener
    {
        readonly ISubject<Monitored<PrivateStream>> _privateStreamListener;
        readonly MqttConfiguration _configuration;

        public PrivateChannelListener( ISubject<Monitored<PrivateStream>> privateStreamListener, MqttConfiguration configuration )
        {
            _privateStreamListener = privateStreamListener;
            _configuration = configuration;
        }

        public IObservable<IMqttChannel<byte[]>> GetChannelStream()
        {
            return _privateStreamListener
                .Select( stream => new PrivateChannel(stream.Monitor, stream.Item, EndpointIdentifier.Server, _configuration ) );
        }

        public void Dispose()
        {
            //Nothing to dispose
        }
    }
}
