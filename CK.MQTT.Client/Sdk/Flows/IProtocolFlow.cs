using System.Threading.Tasks;
using CK.MQTT.Sdk.Packets;

namespace CK.MQTT.Sdk.Flows
{
	internal interface IProtocolFlow
	{
		Task ExecuteAsync (string clientId, IPacket input, IMqttChannel<IPacket> channel);
	}
}
