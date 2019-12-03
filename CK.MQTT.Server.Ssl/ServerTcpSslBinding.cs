using CK.MQTT.Sdk;
using CK.MQTT.Sdk.Bindings;

namespace CK.MQTT.Ssl
{
    /// <summary>
    /// Server binding to use TCP as the underlying MQTT transport protocol
    /// This is the default transport protocol defined by MQTT specification
    /// </summary>
    public class ServerTcpSslBinding : SslTcpBinding, IMqttServerBinding
    {
        private readonly SslTcpConfig _sslTcpConfig;

        public ServerTcpSslBinding( SslTcpConfig sslConfig ) : base (sslConfig)
        {
            _sslTcpConfig = sslConfig;
        }
        /// <summary>
        /// Provides a listener for incoming MQTT channels on top of TCP
        /// </summary>
        /// <param name="config">
        /// The configuration used for creating the listener
        /// See <see cref="MqttConfiguration" /> for more details about the supported values
        /// </param>
        /// <returns>A listener to accept and provide incoming MQTT channels on top of TCP</returns>
        public IMqttChannelListener GetChannelListener( MqttConfiguration config )
            => new GenericListener<GenericChannel>( config, ( conf ) => new SslTcpChannelListener( config, _sslTcpConfig ) );
    }
}
