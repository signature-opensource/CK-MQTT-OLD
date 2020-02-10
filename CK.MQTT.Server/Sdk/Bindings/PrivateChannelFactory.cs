using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk.Bindings
{
    internal class PrivateChannelFactory : IMqttChannelFactory
    {
        readonly ISubject<PrivateStream> _privateStreamListener;
        readonly EndpointIdentifier _identifier;
        readonly MqttConfiguration _configuration;

        public PrivateChannelFactory( ISubject<PrivateStream> privateStreamListener, EndpointIdentifier identifier, MqttConfiguration configuration )
        {
            _privateStreamListener = privateStreamListener;
            _identifier = identifier;
            _configuration = configuration;
        }

        public Task<IMqttChannel<byte[]>> CreateAsync()
        {
            PrivateStream stream = new PrivateStream( _configuration );

            _privateStreamListener.OnNext( stream );

            return Task.FromResult<IMqttChannel<byte[]>>( new PrivateChannel( stream, _identifier, _configuration ) );
        }
    }
}
