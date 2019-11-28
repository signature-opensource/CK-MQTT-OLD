using System.Threading.Tasks;
using CK.MQTT.Sdk.Packets;

namespace CK.MQTT.Sdk
{
	internal interface IPacketManager
	{
		Task<IPacket> GetPacketAsync (byte[] bytes);

		Task<byte[]> GetBytesAsync (IPacket packet);
	}
}
