using System.Threading.Tasks;
using CK.MQTT.Sdk.Packets;
using CK.MQTT.Sdk.Storage;

namespace CK.MQTT.Sdk.Flows
{
	internal interface IPublishSenderFlow : IPublishFlow
	{
		Task SendPublishAsync (string clientId, Publish message, IMqttChannel<IPacket> channel, PendingMessageStatus status = PendingMessageStatus.PendingToSend);
	}
}
