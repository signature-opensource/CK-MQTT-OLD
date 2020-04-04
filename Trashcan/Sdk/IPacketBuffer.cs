using System.Collections.Generic;

namespace CK.MQTT.Sdk
{
    public interface IPacketBuffer
    {
        bool TryGetPackets( IEnumerable<byte> sequence, out IEnumerable<byte[]> packets );
    }
}
