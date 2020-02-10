using CK.MQTT.Sdk.Packets;
using System;

namespace CK.MQTT.Sdk.Formatters
{
    internal class EmptyPacketFormatter<T> : Formatter<T>
        where T : class, IPacket, new()
    {
        readonly MqttPacketType _packetType;

        public EmptyPacketFormatter( MqttPacketType packetType )
        {
            _packetType = packetType;
        }

        public override MqttPacketType PacketType { get { return _packetType; } }

        protected override T Read( byte[] bytes )
        {
            ValidateHeaderFlag( bytes, t => t == _packetType, 0x00 );

            return new T();
        }

        protected override byte[] Write( T packet )
        {
            int flags = 0x00;
            int type = Convert.ToInt32( _packetType ) << 4;

            byte fixedHeaderByte1 = Convert.ToByte( flags | type );
            byte fixedHeaderByte2 = Convert.ToByte( 0x00 );

            return new byte[] { fixedHeaderByte1, fixedHeaderByte2 };
        }
    }
}
