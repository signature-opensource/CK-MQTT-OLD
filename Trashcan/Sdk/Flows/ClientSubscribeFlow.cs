using CK.Core;
using CK.MQTT.Sdk.Packets;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk.Flows
{
    internal class ClientSubscribeFlow : IProtocolFlow
    {
        public Task ExecuteAsync( IActivityMonitor m, string clientId, IPacket input, IMqttChannel<IPacket> channel )
            => Task.CompletedTask;
    }
}
