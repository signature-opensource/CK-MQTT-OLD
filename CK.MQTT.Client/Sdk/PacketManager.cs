using CK.MQTT.Sdk.Formatters;
using CK.MQTT.Sdk.Packets;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk
{
    internal class PacketManager : IPacketManager
    {
        readonly IDictionary<MqttPacketType, IFormatter> formatters;

        public PacketManager( params IFormatter[] formatters )
            : this( (IEnumerable<IFormatter>)formatters )
        {
        }

        public PacketManager( IEnumerable<IFormatter> formatters )
        {
            this.formatters = formatters.ToDictionary( f => f.PacketType );
        }

        public async Task<IPacket> GetPacketAsync( byte[] bytes )
        {
            MqttPacketType packetType = (MqttPacketType)bytes.Byte( 0 ).Bits( 4 );
            if( !formatters.TryGetValue( packetType, out IFormatter formatter ) )
                throw new MqttException( Properties.PacketManager_PacketUnknown );

            IPacket packet = await formatter.FormatAsync( bytes );

            return packet;
        }

        public async Task<byte[]> GetBytesAsync( IPacket packet )
        {
            if( !formatters.TryGetValue( packet.Type, out IFormatter formatter ) )
                throw new MqttException( Properties.PacketManager_PacketUnknown );

            byte[] bytes = await formatter.FormatAsync( packet );

            return bytes;
        }
    }
}
