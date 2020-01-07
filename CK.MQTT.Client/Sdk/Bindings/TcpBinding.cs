namespace CK.MQTT.Sdk.Bindings
{
	/// <summary>
	/// Binding to use TCP as the underlying MQTT transport protocol
	/// This is the default transport protocol defined by MQTT specification
	/// </summary>
	public class TcpBinding : IMqttBinding
	{
        /// <summary>
        /// Provides a factory for MQTT channels on top of TCP
        /// </summary>
        /// <param name="connectionString">Host name or IP address to connect the channels</param>
        /// <param name="configuration">
        /// The configuration used for creating the factory and channels
        /// See <see cref="MqttConfiguration" /> for more details about the supported values
        /// </param>
        /// <returns>A factory for creating MQTT channels on top of TCP</returns>
		public IMqttChannelFactory GetChannelFactory (string connectionString, MqttConfiguration configuration)
			=> new TcpChannelFactory (connectionString, configuration);
	}
}
