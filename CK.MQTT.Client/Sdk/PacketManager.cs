
using CK.Core;
using CK.MQTT.Sdk.Formatters;
using CK.MQTT.Sdk.Packets;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk
{
    internal class PacketManager : IPacketManager
    {
        readonly IDictionary<MqttPacketType, IFormatter> _formatters;

        public PacketManager( params IFormatter[] formatters )
            : this( (IEnumerable<IFormatter>)formatters )
        {
        }

        public PacketManager( IEnumerable<IFormatter> formatters )
        {
            _formatters = formatters.ToDictionary( f => f.PacketType );
        }

        public async Task<Monitored<IPacket>> GetPacketAsync( Monitored<byte[]> bytes )
        {
            MqttPacketType packetType = (MqttPacketType)bytes.Item.Byte( 0 ).Bits( 4 );
            if( !_formatters.TryGetValue( packetType, out IFormatter formatter ) )
                throw new MqttException( ClientProperties.PacketManager_PacketUnknown );

            IPacket packet = await formatter.FormatAsync( bytes.Item );

            return new Monitored<IPacket>( bytes.Monitor, packet );
        }

        public async Task<Monitored<byte[]>> GetBytesAsync( Monitored<IPacket> packet )
        {
            if( !_formatters.TryGetValue( packet.Item.Type, out IFormatter formatter ) )
                throw new MqttException( ClientProperties.PacketManager_PacketUnknown );

            byte[] bytes = await formatter.FormatAsync( packet.Item );

            return new Monitored<byte[]>( packet.Monitor, bytes );
        }
    }
}
