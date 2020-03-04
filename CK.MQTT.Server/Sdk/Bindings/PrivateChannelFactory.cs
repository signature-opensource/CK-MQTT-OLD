using CK.Core;

using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk.Bindings
{
    internal class PrivateChannelFactory : IMqttChannelFactory
    {
        readonly ISubject<Monitored<PrivateStream>> _privateStreamListener;
        readonly EndpointIdentifier _identifier;
        readonly MqttConfiguration _configuration;

        public PrivateChannelFactory( ISubject<Monitored<PrivateStream>> privateStreamListener, EndpointIdentifier identifier, MqttConfiguration configuration )
        {
            _privateStreamListener = privateStreamListener;
            _identifier = identifier;
            _configuration = configuration;
        }

        public Task<IMqttChannel<byte[]>> CreateAsync( IActivityMonitor m )
        {
            PrivateStream stream = new PrivateStream( _configuration );
            var monitor = new ActivityMonitor();
            _privateStreamListener.OnNext( new Monitored<PrivateStream>( monitor, stream ) );

            return Task.FromResult<IMqttChannel<byte[]>>( new PrivateChannel( monitor, stream, _identifier, _configuration ) );
        }
    }
}
