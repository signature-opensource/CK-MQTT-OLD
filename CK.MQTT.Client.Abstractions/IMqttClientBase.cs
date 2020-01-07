using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CK.MQTT
{
    public interface IMqttClientBase
    {
        /// <summary>
		/// Represents the incoming application messages received from the Server
		/// These messages correspond to the topics subscribed,
		/// by calling <see cref="SubscribeAsync(string, MqttQualityOfService)"/> method.
		/// See <see cref="MqttApplicationMessage"/> for more details about the application messages.
		/// </summary>
		/// <remarks>
		/// The stream lifecycle depends on the implementation. Some implementations require to resubscribe when the
        /// <see cref="IMqttClient"/> get disconnected.
		/// </remarks>
		IObservable<MqttApplicationMessage> MessageStream { get; }

        /// <summary>
		/// Indicates if the Client is currently connected.
		/// </summary>
		bool IsConnected { get; }

        /// <summary>
        /// Represents the unsubscription of a topic.
        /// Depending on the implementation the unsubscription may be stored and not be directly sent to the client,
        /// but the implementation should guarantee that no <see cref="MqttApplicationMessage"/> of an unsubscribed topic
        /// should come after the unsubscription.
		/// </summary>
		/// <param name="topics">
		/// The list of topics to unsubscribe from
		/// Once the unsubscription completes, no more application messages for those topics will arrive to <see cref="MessageStream"/> 
		/// </param>
		Task UnsubscribeAsync( params string[] topics );


        /// <summary>
		/// Publish the message. Some implementations may store the messages and send them later to the server.
        /// The QoS should be respected in any implementation.
		/// </summary>
		/// <param name="message">
		/// The application message to publish to the Server.
		/// See <see cref="MqttApplicationMessage" /> for more details about the application messages
		/// </param>
		/// <param name="qos">
		/// The Quality Of Service (QoS) associated to the application message, which determines
        /// the behavior of the implementation to guarantee this QoS.
		/// See <see cref="MqttQualityOfService" /> for more details about the QoS values
		/// </param>
		/// <param name="retain">
		/// Indicates if the application message should be retained by the Server for future subscribers.
		/// Only the last message of each topic is retained
		/// </param>
		/// <remarks>
		/// See <a href="http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/mqtt-v3.1.1.html#_Toc442180850">MQTT Publish</a>
		/// for more details about the protocol publish
		/// </remarks>
		Task PublishAsync( MqttApplicationMessage message, MqttQualityOfService qos, bool retain = false );

        /// <summary>
        /// Represents the subscription of a topic.
        /// Depending on the implementation, the subscription may be stored and sent later.
        /// </summary>
        /// <param name="topicFilter">
        /// The topic to subscribe for incoming application messages. 
        /// Every message sent by the Server that matches a subscribed topic, will go to the <see cref="MessageStream"/> 
        /// </param>
        /// <param name="qos">
        /// The maximum Quality Of Service (QoS) that the Server should maintain when publishing application messages for the subscribed topic to the Client
        /// This QoS is maximum because it depends on the QoS supported by the Server. 
        /// See <see cref="MqttQualityOfService" /> for more details about the QoS values
        /// </param>
        /// <remarks>
        /// See <a href="http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/mqtt-v3.1.1.html#_Toc442180876">MQTT Subscribe</a>
        /// for more details about the protocol subscription
        /// </remarks>
        Task SubscribeAsync( string topicFilter, MqttQualityOfService qos );

        /// <summary>
        /// Event raised when the Client gets disconnected.
        /// The Client disconnection could be caused by a protocol disconnect, 
        /// an error or a remote disconnection produced by the Server.
        /// See <see cref="MqttEndpointDisconnected"/> for more details on the disconnection information
        /// </summary>
        event EventHandler<MqttEndpointDisconnected> Disconnected;

    }
}
