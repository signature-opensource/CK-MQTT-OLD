using CK.Core;
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
        readonly SslTcpConfig _sslTcpConfig;
        readonly ServerSslConfig _config;

        public ServerTcpSslBinding( SslTcpConfig sslConfig, ServerSslConfig config ) : base( sslConfig )
        {
            _sslTcpConfig = sslConfig;
            _config = config;
        }
        /// <summary>
        /// Provides a listener for incoming MQTT channels on top of TCP
        /// </summary>
        /// <param name="config">
        /// The configuration used for creating the listener
        /// See <see cref="MqttConfiguration" /> for more details about the supported values
        /// </param>
        /// <returns>A listener to accept and provide incoming MQTT channels on top of TCP</returns>
        public IMqttChannelListener GetChannelListener( IActivityMonitor m, MqttConfiguration config )
            => new GenericListener<GenericChannel>( config, ( conf ) => new SslTcpChannelListener( config, _sslTcpConfig, _config ) );
    }
}
