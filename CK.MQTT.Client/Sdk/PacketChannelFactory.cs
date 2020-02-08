using System.Collections.Generic;
using CK.MQTT.Sdk.Formatters;
using CK.MQTT.Sdk.Packets;
using System.Threading.Tasks;
using CK.Core;

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

        public async Task<IMqttChannel<IPacket>> CreateAsync( IActivityMonitor m )
        {
            if( _innerChannelFactory == null )
            {
                throw new MqttException( Properties.PacketChannelFactory_InnerChannelFactoryNotFound );
            }

            var binaryChannel = await _innerChannelFactory
                .CreateAsync( m )
                .ConfigureAwait( continueOnCapturedContext: false );

            return Create( m, binaryChannel );
        }

        public IMqttChannel<IPacket> Create( IActivityMonitor m, IMqttChannel<Monitored<byte[]>> binaryChannel )
        {
            var formatters = GetFormatters();
            var packetManager = new PacketManager( formatters );

            return new PacketChannel( binaryChannel, packetManager, _configuration );
        }

        IEnumerable<IFormatter> GetFormatters()
        {
            var formatters = new List<IFormatter>();

            formatters.Add( new ConnectFormatter() );
            formatters.Add( new ConnectAckFormatter() );
            formatters.Add( new PublishFormatter( _topicEvaluator ) );
            formatters.Add( new FlowPacketFormatter<PublishAck>( MqttPacketType.PublishAck, id => new PublishAck( id ) ) );
            formatters.Add( new FlowPacketFormatter<PublishReceived>( MqttPacketType.PublishReceived, id => new PublishReceived( id ) ) );
            formatters.Add( new FlowPacketFormatter<PublishRelease>( MqttPacketType.PublishRelease, id => new PublishRelease( id ) ) );
            formatters.Add( new FlowPacketFormatter<PublishComplete>( MqttPacketType.PublishComplete, id => new PublishComplete( id ) ) );
            formatters.Add( new SubscribeFormatter( _topicEvaluator ) );
            formatters.Add( new SubscribeAckFormatter() );
            formatters.Add( new UnsubscribeFormatter() );
            formatters.Add( new FlowPacketFormatter<UnsubscribeAck>( MqttPacketType.UnsubscribeAck, id => new UnsubscribeAck( id ) ) );
            formatters.Add( new EmptyPacketFormatter<PingRequest>( MqttPacketType.PingRequest ) );
            formatters.Add( new EmptyPacketFormatter<PingResponse>( MqttPacketType.PingResponse ) );
            formatters.Add( new EmptyPacketFormatter<Disconnect>( MqttPacketType.Disconnect ) );

            return formatters;
        }
    }
}
