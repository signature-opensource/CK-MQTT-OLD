using System.Threading.Tasks;
using CK.MQTT.Sdk.Packets;

namespace CK.MQTT.Sdk.Flows
{
	internal class PingFlow : IProtocolFlow
	{
		public async Task ExecuteAsync (string clientId, IPacket input, IMqttChannel<IPacket> channel)
		{
			if (input.Type != MqttPacketType.PingRequest) {
				return;
			}

			await channel.SendAsync (new PingResponse ())
				.ConfigureAwait (continueOnCapturedContext: false);
		}
	}
}
