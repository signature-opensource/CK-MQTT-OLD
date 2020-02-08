using System.Threading.Tasks;
using CK.Core;
using CK.MQTT.Sdk.Packets;

namespace CK.MQTT.Sdk.Flows
{
	internal interface IProtocolFlow
	{
		Task ExecuteAsync ( IActivityMonitor m, string clientId, IPacket input, IMqttChannel<IPacket> channel);
	}
}
