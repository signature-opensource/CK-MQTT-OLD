using CK.Core;
using CK.MQTT.Sdk;
using System;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk.Bindings
{
    public class GenericChannelFactory : IMqttChannelFactory
    {
        readonly Func<IActivityMonitor,Task<IChannelClient>> _channelClientFactory;
        readonly MqttConfiguration _configuration;

        public GenericChannelFactory( Func<IActivityMonitor,Task<IChannelClient>> channelClientFactory, MqttConfiguration configuration )
        {
            _channelClientFactory = channelClientFactory;
            _configuration = configuration;
        }
        public async Task<IMqttChannel<byte[]>> CreateAsync( IActivityMonitor m )
        {
            return new GenericChannel(await _channelClientFactory(m), new PacketBuffer(), _configuration);
        }
    }
}
