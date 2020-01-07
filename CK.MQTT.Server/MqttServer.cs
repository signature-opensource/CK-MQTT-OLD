using CK.MQTT.Sdk;
using CK.MQTT.Sdk.Bindings;

namespace CK.MQTT
{
	/// <summary>
	/// Creates instances of <see cref="IMqttServer"/> for accepting connections from 
	/// MQTT clients.
	/// </summary>
	public static class MqttServer
	{
		/// <summary>
		/// Creates an <see cref="IMqttServer"/> using the specified MQTT configuration 
		/// to customize the protocol parameters, and an optional transport binding and authentication provider.
		/// </summary>
		/// <param name="configuration">
		/// The configuration used for creating the Server.
		/// See <see cref="MqttConfiguration" /> for more details about the supported values.
		/// </param>
		/// <param name="binding">
		/// The binding to use as the underlying transport layer.
		/// Deafault value: <see cref="ServerTcpBinding"/>
		/// See <see cref="IMqttServerBinding"/> for more details about how 
		/// to implement a custom binding
		/// </param>
		/// <param name="authenticationProvider">
		/// Optional authentication provider to use, 
		/// to enable authentication as part of the connection mechanism.
		/// See <see cref="IMqttAuthenticationProvider" /> for more details about how to implement
		/// an authentication provider.
		/// </param>
		/// <returns>A new MQTT Server</returns>
		/// <exception cref="MqttServerException">MqttServerException</exception>
		public static IMqttServer Create (MqttConfiguration configuration, IMqttServerBinding binding = null, IMqttAuthenticationProvider authenticationProvider = null)
			=> new MqttServerFactory (binding ?? new ServerTcpBinding(MqttProtocol.DefaultNonSecurePort), authenticationProvider).CreateServer (configuration);

		/// <summary>
		/// Creates an <see cref="IMqttServer"/> over the TCP protocol, using the 
		/// MQTT protocol defaults.
		/// </summary>
		/// <returns>A new MQTT Server</returns>
		/// <exception cref="MqttServerException">MqttServerException</exception>
		public static IMqttServer Create () => new MqttServerFactory ().CreateServer (new MqttConfiguration ());
	}
}
