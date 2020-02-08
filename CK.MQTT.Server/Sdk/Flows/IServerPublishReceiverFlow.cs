using CK.Core;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk.Flows
{
    internal interface IServerPublishReceiverFlow : IProtocolFlow
    {
        Task SendWillAsync ( IActivityMonitor m, string clientId );
    }
}
