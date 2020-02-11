using CK.MQTT.Sdk.Formatters;
using CK.MQTT.Sdk.Packets;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk
{
    internal class PacketChannelFactory : IPacketChannelFactory
    {
        readonly IMqttChannelFactory _innerChannelFactory;
        readonly IMqttTopicEvaluator _topicEvaluator;
        readonly MqttConfiguration _configuration;

        public PacketChannelFactory( IMqttChannelFactory innerChannelFactory,
            IMqttTopicEvaluator topicEvaluator,
            MqttConfiguration configuration )
            : this( topicEvaluator, configuration )
        {
            _innerChannelFactory = innerChannelFactory;
        }

        public PacketChannelFactory( IMqttTopicEvaluator topicEvaluator,
            MqttConfiguration configuration )
        {
            _topicEvaluator = topicEvaluator;
            _configuration = configuration;
        }

        public async Task<IMqttChannel<IPacket>> CreateAsync()
        {
            if( _innerChannelFactory == null )
            {
                throw new MqttException( Properties.PacketChannelFactory_InnerChannelFactoryNotFound );
            }

            IMqttChannel<byte[]> binaryChannel = await _innerChannelFactory
                .CreateAsync();

            return Create( binaryChannel );
        }

        public IMqttChannel<IPacket> Create( IMqttChannel<byte[]> binaryChannel )
        {
            IEnumerable<IFormatter> formatters = GetFormatters();
            PacketManager packetManager = new PacketManager( formatters );

            return new PacketChannel( binaryChannel, packetManager, _configuration );
        }

        IEnumerable<IFormatter> GetFormatters()
        {
            List<IFormatter> formatters = new List<IFormatter>
            {
                new ConnectFormatter(),
                new ConnectAckFormatter(),
                new PublishFormatter( _topicEvaluator ),
                new FlowPacketFormatter<PublishAck>( MqttPacketType.PublishAck, id => new PublishAck( id ) ),
                new FlowPacketFormatter<PublishReceived>( MqttPacketType.PublishReceived, id => new PublishReceived( id ) ),
                new FlowPacketFormatter<PublishRelease>( MqttPacketType.PublishRelease, id => new PublishRelease( id ) ),
                new FlowPacketFormatter<PublishComplete>( MqttPacketType.PublishComplete, id => new PublishComplete( id ) ),
                new SubscribeFormatter( _topicEvaluator ),
                new SubscribeAckFormatter(),
                new UnsubscribeFormatter(),
                new FlowPacketFormatter<UnsubscribeAck>( MqttPacketType.UnsubscribeAck, id => new UnsubscribeAck( id ) ),
                new EmptyPacketFormatter<PingRequest>( MqttPacketType.PingRequest ),
                new EmptyPacketFormatter<PingResponse>( MqttPacketType.PingResponse ),
                new EmptyPacketFormatter<Disconnect>( MqttPacketType.Disconnect )
            };
            return formatters;
        }
    }
}
