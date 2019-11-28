using System.Reactive.Subjects;

namespace CK.MQTT.Sdk.Bindings
{
    internal class PrivateBinding : IMqttBinding
    {
        readonly ISubject<PrivateStream> privateStreamListener;
        readonly EndpointIdentifier identifier;

        public PrivateBinding (ISubject<PrivateStream> privateStreamListener, EndpointIdentifier identifier)
        {
            this.privateStreamListener = privateStreamListener;
            this.identifier = identifier;
        }

        public IMqttChannelFactory GetChannelFactory (string hostAddress, MqttConfiguration configuration)
        {
            return new PrivateChannelFactory (privateStreamListener, identifier, configuration);
        }
    }
}
