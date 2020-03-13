using System;

namespace CK.MQTT
{
    /// <summary>
    /// Represents an application message, which correspond to the unit of information
    /// sent from Client to Server and from Server to Client
    /// </summary>
	public class MqttApplicationMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MqttApplicationMessage" /> class,
        /// specifying the topic and payload of the message
        /// </summary>
        /// <param name="topic">
        /// Topic associated with the message
        /// Any subscriber of this topic should receive the corresponding messages
        /// </param>
        /// <param name="payload">Content of the message, as a byte array</param>
		public MqttApplicationMessage( string topic, ReadOnlyMemory<byte> payload )
        {
            Topic = topic;
            Payload = payload;
        }

        /// <summary>
        /// Topic associated with the message.
        /// Any subscriber of this topic should receive the corresponding messages.
        /// </summary>
		public string Topic { get; }

        /// <summary>
        /// Content of the message. This must be accessed only during the handling of the message.
        /// </summary>
		public ReadOnlyMemory<byte> Payload { get; }
    }
}
