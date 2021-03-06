using CK.Core;

using CK.MQTT.Sdk.Packets;
using CK.MQTT.Sdk.Storage;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk.Flows
{
    internal interface IPublishFlow : IProtocolFlow
    {
        Task SendAckAsync( IActivityMonitor m, string clientId, IFlowPacket ack, IMqttChannel<IPacket> channel, PendingMessageStatus status = PendingMessageStatus.PendingToSend );
    }
}
