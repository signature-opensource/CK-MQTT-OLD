using System.Threading.Tasks;
using CK.MQTT.Sdk.Packets;
using CK.MQTT.Sdk.Storage;

namespace CK.MQTT.Sdk.Flows
{
	internal interface IPublishFlow : IProtocolFlow
	{
		Task SendAckAsync (string clientId, IFlowPacket ack, IMqttChannel<IPacket> channel, PendingMessageStatus status = PendingMessageStatus.PendingToSend);
	}
}
