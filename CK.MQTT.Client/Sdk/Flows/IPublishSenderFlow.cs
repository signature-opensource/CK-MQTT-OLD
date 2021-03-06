using CK.Core;

using CK.MQTT.Sdk.Packets;
using CK.MQTT.Sdk.Storage;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk.Flows
{
    internal interface IPublishSenderFlow : IPublishFlow
    {
        Task SendPublishAsync( IActivityMonitor m, string clientId, Publish message, IMqttChannel<IPacket> channel, PendingMessageStatus status = PendingMessageStatus.PendingToSend );
    }
}
