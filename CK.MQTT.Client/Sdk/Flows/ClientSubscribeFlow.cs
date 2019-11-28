using System.Threading.Tasks;
using CK.MQTT.Sdk.Packets;

namespace CK.MQTT.Sdk.Flows
{
	internal class ClientSubscribeFlow : IProtocolFlow
	{
		public Task ExecuteAsync (string clientId, IPacket input, IMqttChannel<IPacket> channel)
		{
			return Task.Delay (0);
		}
	}
}
