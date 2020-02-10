using System;
using System.IO;

namespace CK.MQTT.Ssl
{
    public interface IChannelClient : IDisposable
    {

        /// <summary>
        /// Gets that this client is ready to send data.
        /// </summary>
        bool Connected { get; }

        /// <summary>
        /// Returns the <see cref="Stream"/> used to send and receive data.
        /// </summary>
        Stream GetStream();

        /// <summary>
        /// Indication of a buffer size to use.
        /// This may be not respected.
        /// </summary>
        int PreferedSendBufferSize { get; set; }

        /// <summary>
        /// Indication of a buffer size to use.
        /// This may be not respected.
        /// </summary>
        int PreferedReceiveBufferSize { get; set; }
    }
}
