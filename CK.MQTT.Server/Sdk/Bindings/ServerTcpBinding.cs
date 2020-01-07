namespace CK.MQTT.Sdk.Bindings
{
	/// <summary>
	/// Server binding to use TCP as the underlying MQTT transport protocol
	/// This is the default transport protocol defined by MQTT specification
	/// </summary>
	public class ServerTcpBinding : TcpBinding, IMqttServerBinding
	{
        readonly int _port;
        public ServerTcpBinding(int port)
        {
            _port = port;
        }

        public ServerTcpBinding()
        {
            _port = MqttProtocol.DefaultNonSecurePort;
        }

		/// <summary>
		/// Provides a listener for incoming MQTT channels on top of TCP
		/// </summary>
		/// <param name="configuration">
		/// The configuration used for creating the listener
		/// See <see cref="MqttConfiguration" /> for more details about the supported values
		/// </param>
		/// <returns>A listener to accept and provide incoming MQTT channels on top of TCP</returns>
		public IMqttChannelListener GetChannelListener (MqttConfiguration configuration)
			=> new TcpChannelListener (_port, configuration);
	}
}
