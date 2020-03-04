using CK.Core;
using System.Reactive.Subjects;

namespace CK.MQTT.Sdk.Bindings
{
    internal class PrivateBinding : IMqttBinding
    {
        readonly ISubject<Monitored<PrivateStream>> _privateStreamListener;
        readonly EndpointIdentifier _identifier;

        public PrivateBinding( ISubject<Monitored<PrivateStream>> privateStreamListener, EndpointIdentifier identifier )
        {
            _privateStreamListener = privateStreamListener;
            _identifier = identifier;
        }

        public IMqttChannelFactory GetChannelFactory( string hostAddress, MqttConfiguration configuration )
        {
            return new PrivateChannelFactory( _privateStreamListener, _identifier, configuration );
        }
    }
}
