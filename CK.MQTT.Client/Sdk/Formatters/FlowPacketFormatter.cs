using CK.MQTT.Sdk.Packets;
using System;

namespace CK.MQTT.Sdk.Formatters
{
    internal class FlowPacketFormatter<T> : Formatter<T>
        where T : class, IFlowPacket
    {
        readonly MqttPacketType _packetType;
        readonly Func<ushort, T> _packetFactory;

        public FlowPacketFormatter( MqttPacketType packetType, Func<ushort, T> packetFactory )
        {
            _packetType = packetType;
            _packetFactory = packetFactory;
        }

        public override MqttPacketType PacketType { get { return _packetType; } }

        protected override T Read( byte[] bytes )
        {
            ValidateHeaderFlag( bytes, t => t == MqttPacketType.PublishRelease, 0x02 );
            ValidateHeaderFlag( bytes, t => t != MqttPacketType.PublishRelease, 0x00 );


            MqttProtocol.Encoding.DecodeRemainingLength( bytes, out int remainingLengthBytesLength );

            int packetIdIndex = MqttProtocol.PacketTypeLength + remainingLengthBytesLength;
            byte[] packetIdBytes = bytes.Bytes( packetIdIndex, 2 );

            return _packetFactory( packetIdBytes.ToUInt16() );
        }

        protected override byte[] Write( T packet )
        {
            byte[] variableHeader = MqttProtocol.Encoding.EncodeInteger( packet.PacketId );
            byte[] remainingLength = MqttProtocol.Encoding.EncodeRemainingLength( variableHeader.Length );
            byte[] fixedHeader = GetFixedHeader( packet.Type, remainingLength );
            byte[] bytes = new byte[fixedHeader.Length + variableHeader.Length];

            fixedHeader.CopyTo( bytes, 0 );
            variableHeader.CopyTo( bytes, fixedHeader.Length );

            return bytes;
        }

        byte[] GetFixedHeader( MqttPacketType packetType, byte[] remainingLength )
        {
            // MQTT 2.2.2: http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/csprd02/mqtt-v3.1.1-csprd02.html#_Toc385349758
            // The flags for PUBREL are different than for the other flow packets.
            int flags = packetType == MqttPacketType.PublishRelease ? 0x02 : 0x00;
            int type = Convert.ToInt32( packetType ) << 4;
            byte fixedHeaderByte1 = Convert.ToByte( flags | type );
            byte[] fixedHeader = new byte[1 + remainingLength.Length];

            fixedHeader[0] = fixedHeaderByte1;
            remainingLength.CopyTo( fixedHeader, 1 );

            return fixedHeader;
        }
    }
}
