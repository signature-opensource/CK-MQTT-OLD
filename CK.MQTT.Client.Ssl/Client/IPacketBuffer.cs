using System.Collections.Generic;

namespace ConsoleApp1
{
	internal interface IPacketBuffer
	{
		bool TryGetPackets(IEnumerable<byte> sequence, out IEnumerable<byte[]> packets);
	}
}
