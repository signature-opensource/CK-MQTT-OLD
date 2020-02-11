using CK.MQTT.Sdk.Packets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CK.MQTT.Sdk.Formatters
{
    internal class SubscribeAckFormatter : Formatter<SubscribeAck>
    {
        public override MqttPacketType PacketType => MqttPacketType.SubscribeAck;

        protected override SubscribeAck Read( byte[] bytes )
        {
            ValidateHeaderFlag( bytes, t => t == MqttPacketType.SubscribeAck, 0x00 );

            int remainingLength = MqttProtocol.Encoding.DecodeRemainingLength( bytes, out int remainingLengthBytesLength );

            int packetIdentifierStartIndex = remainingLengthBytesLength + 1;
            ushort packetIdentifier = bytes.Bytes( packetIdentifierStartIndex, 2 ).ToUInt16();

            int headerLength = 1 + remainingLengthBytesLength + 2;
            byte[] returnCodeBytes = bytes.Bytes( headerLength );

            if( !returnCodeBytes.Any() )
                throw new MqttProtocolViolationException( Properties.SubscribeAckFormatter_MissingReturnCodes );

            if( returnCodeBytes.Any( b => !Enum.IsDefined( typeof( SubscribeReturnCode ), b ) ) )
                throw new MqttProtocolViolationException( Properties.SubscribeAckFormatter_InvalidReturnCodes );

            SubscribeReturnCode[] returnCodes = returnCodeBytes.Select( b => (SubscribeReturnCode)b ).ToArray();

            return new SubscribeAck( packetIdentifier, returnCodes );
        }

        protected override byte[] Write( SubscribeAck packet )
        {
            List<byte> bytes = new List<byte>();

            byte[] variableHeader = GetVariableHeader( packet );
            byte[] payload = GetPayload( packet );
            byte[] remainingLength = MqttProtocol.Encoding.EncodeRemainingLength( variableHeader.Length + payload.Length );
            byte[] fixedHeader = GetFixedHeader( remainingLength );

            bytes.AddRange( fixedHeader );
            bytes.AddRange( variableHeader );
            bytes.AddRange( payload );

            return bytes.ToArray();
        }

        byte[] GetFixedHeader( byte[] remainingLength )
        {
            List<byte> fixedHeader = new List<byte>();

            int flags = 0x00;
            int type = Convert.ToInt32( MqttPacketType.SubscribeAck ) << 4;

            byte fixedHeaderByte1 = Convert.ToByte( flags | type );

            fixedHeader.Add( fixedHeaderByte1 );
            fixedHeader.AddRange( remainingLength );

            return fixedHeader.ToArray();
        }

        byte[] GetVariableHeader( SubscribeAck packet )
        {
            List<byte> variableHeader = new List<byte>();

            byte[] packetIdBytes = MqttProtocol.Encoding.EncodeInteger( packet.PacketId );

            variableHeader.AddRange( packetIdBytes );

            return variableHeader.ToArray();
        }

        byte[] GetPayload( SubscribeAck packet )
        {
            if( packet.ReturnCodes == null || !packet.ReturnCodes.Any() )
                throw new MqttProtocolViolationException( Properties.SubscribeAckFormatter_MissingReturnCodes );

            return packet.ReturnCodes
                .Select( c => Convert.ToByte( c ) )
                .ToArray();
        }
    }
}
