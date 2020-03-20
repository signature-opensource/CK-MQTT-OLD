using CK.Core;

using System;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk
{
    /// <summary>
    /// Represents a mechanism to send and receive information between two endpoints
    /// </summary>
    /// <typeparam name="T">The type of information that will go through the channel</typeparam>
    public interface IMqttChannel<T> : IDisposable
    {
        /// <summary>
        /// Indicates if the channel is connected to the underlying stream or not
        /// </summary>
		bool IsConnected { get; }

        /// <summary>
        /// Represents the stream of incoming information received by the channel
        /// </summary>
		IObservable<Mon<T>> ReceiverStream { get; }

        /// <summary>
        /// Represents the stream of outgoing information sent by the channel
        /// </summary>
		IObservable<Mon<T>> SenderStream { get; }

        /// <summary>
        /// Sends information to the other end, through the underlying stream
        /// </summary>
        /// <param name="message">
        /// Message to send to the other end of the channel
        /// </param>
		/// <exception cref="MqttException">MqttException</exception>
		Task SendAsync( Mon<T> message );
    }
}
