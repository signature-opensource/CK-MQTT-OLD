using CK.MQTT.Sdk;
using System;
using System.Threading.Tasks;

namespace CK.MQTT.Ssl
{
    class GenericChannelFactory : IMqttChannelFactory
    {
        readonly Func<Task<IChannelClient>> _channelClientFactory;
        readonly MqttConfiguration _configuration;

        public GenericChannelFactory( Func<Task<IChannelClient>> channelClientFactory, MqttConfiguration configuration )
        {
            _channelClientFactory = channelClientFactory;
            _configuration = configuration;
        }
        public async Task<IMqttChannel<byte[]>> CreateAsync()
        {
            return new GenericChannel( await _channelClientFactory(), new PacketBuffer(), _configuration );
        }
    }
}
