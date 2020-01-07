using System;
using System.Threading.Tasks;

namespace CK.MQTT
{
	/// <summary>
	/// Represents an MQTT Client
	/// </summary>
	public interface IMqttClient : IMqttClientBase, IDisposable
	{

		/// <summary>
		/// Id of the connected Client.
		/// This Id correspond to the <see cref="MqttClientCredentials.ClientId"/> parameter passed to 
		/// <see cref="ConnectAsync (MqttClientCredentials, MqttLastWill, bool)"/> method
		/// </summary>
		string Id { get; }

		/// <summary>
		/// Represents the protocol connection, which consists of sending a CONNECT packet
		/// and awaiting the corresponding CONNACK packet from the Server
		/// </summary>
		/// <param name="credentials">
		/// The credentials used to connect to the Server. 
		/// See <see cref="MqttClientCredentials" /> for more details on the credentials information
		/// </param>
		/// <param name="will">
		/// The last will message to send from the Server when an unexpected Client disconnection occurrs. 
		/// See <see cref="MqttLastWill" /> for more details about the will message structure
		/// </param>
		/// <param name="cleanSession">
		/// Indicates if the session state between Client and Server must be cleared between connections
		/// Defaults to false, meaning that session state will be preserved by default accross connections
		/// </param>
		/// <returns>
		/// Returns the state of the client session created as part of the connection
		/// See <see cref="SessionState" /> for more details about the session state values
		/// </returns>
		/// <exception cref="MqttClientException">MqttClientException</exception>
		/// <remarks>
		/// See <a href="http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/mqtt-v3.1.1.html#_Toc442180841">MQTT Connect</a>
		/// for more details about the protocol connection
		/// </remarks>
		Task<SessionState> ConnectAsync(MqttClientCredentials credentials, MqttLastWill will = null, bool cleanSession = false);

		/// <summary>
		/// Represents the protocol connection, which consists of sending a CONNECT packet
		/// and awaiting the corresponding CONNACK packet from the Server.
		/// This overload allows to connect without specifying a client id, in which case a random id will be generated
		/// according to the specification of the protocol.
		/// </summary>
		/// <param name="will">
		/// The last will message to send from the Server when an unexpected Client disconnection occurrs. 
		/// See <see cref="MqttLastWill" /> for more details about the will message structure
		/// </param>
		/// /// <returns>
		/// Returns the state of the client session created as part of the connection
		/// See <see cref="SessionState" /> for more details about the session state values
		/// </returns>
		/// <exception cref="MqttClientException">MqttClientException</exception>
		/// <remarks>
		/// See <a href="http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/mqtt-v3.1.1.html#_Toc442180841">MQTT Connect</a>
		/// for more details about the protocol connection
		/// </remarks>
		Task<SessionState> ConnectAsync(MqttLastWill will = null);

		/// <summary>
		/// Represents the protocol disconnection, which consists of sending a DISCONNECT packet to the Server
		/// No acknowledgement is sent by the Server on the disconnection
		/// Once the client is successfully disconnected, the <see cref="Disconnected"/> event will be fired 
		/// </summary>
		/// <remarks>
		/// See <a href="http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/mqtt-v3.1.1.html#_Toc442180903">MQTT Disconnect</a>
		/// for more details about the protocol disconnection
		/// </remarks>
		Task DisconnectAsync();
	}
}
