using System.Collections.Generic;

namespace CK.MQTT.Sdk
{
    internal interface IPacketBuffer
	{
		bool TryGetPackets (IEnumerable<byte> sequence, out IEnumerable<byte[]> packets);
	}
}
