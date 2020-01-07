using Microsoft.Extensions.Hosting;

namespace CK.MQTT.Client.Abstractions
{
    interface ISimpleMqttClientService : ISimpleMqttClient, IHostedService
    {
    }
}
