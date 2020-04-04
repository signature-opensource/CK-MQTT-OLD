using CK.MQTT.Common.Packets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CK.MQTT.Common
{
    public interface IMessageStore
    {
        /// <summary>
        /// Store a message in the session, return a packet identifier to use in the QoS flow.
        /// http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/errata01/os/mqtt-v3.1.1-errata01-os-complete.html#_Toc442180912
        /// </summary>
        /// <returns>An unused packet identifier</returns>
        ValueTask<ushort> StoreMessageAsync( ApplicationMessage message, QualityOfService qos );

        /// <summary>
        /// Discard a message. The packet ID will be freed if the QoS of the stored message is <see cref="QualityOfService.AtMostOnce"/>.
        /// </summary>
        /// <param name="packetId">The packet ID of the message to discard.</param>
        /// <param name="freePacketId"></param>
        /// <returns><see langword="true"/> if the packet ID have been freed (QoS at least once).</returns>
        ValueTask<QualityOfService> DiscardMessageFromIdAsync( ushort packetId );

        /// <summary>
        /// Free a packet identifier so it can be reused.
        /// To be called only on message stored with <see cref="QualityOfService.ExactlyOnce"/> that have been already discarded.
        /// </summary>
        /// <param name="packetId">The packetId to freed</param>
        /// <returns></returns>
        ValueTask FreePacketIdentifier( ushort packetId );

        IEnumerable<ushort> OrphansPacketsId { get; }

        IEnumerable<StoredApplicationMessage> AllStoredMessages { get; }

    }

    public readonly struct StoredApplicationMessage
    {
        public StoredApplicationMessage( ApplicationMessage applicationMessage, QualityOfService qualityOfService, ushort packetId )
        {
            ApplicationMessage = applicationMessage;
            QualityOfService = qualityOfService;
            PacketId = packetId;
        }
        public readonly ApplicationMessage ApplicationMessage;
        public readonly QualityOfService QualityOfService;
        public readonly ushort PacketId;
    }
}
