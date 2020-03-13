using CK.Core;

using System;

namespace CK.MQTT
{
    public class MqttMessageEventArgs : EventMonitoredArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MqttMessageEventArgs" /> class,
        /// specifying the topic and payload of the message.
        /// </summary>
        /// <param name="m">The monitor that should be used during this processing.</param>
        /// <param name="topic">Topic associated with the message. Any subscriber of this topic should receive the corresponding messages.</param>
        /// <param name="payload">Content of the message.</param>
		public MqttMessageEventArgs( IActivityMonitor m, string topic, ReadOnlyMemory<byte> payload )
            : base( m )
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
        /// Content of the message.
        /// </summary>
		public ReadOnlyMemory<byte> Payload { get; }
    }
}
