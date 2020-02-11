using CK.MQTT.Sdk.Packets;
using System;

namespace CK.MQTT.Sdk.Formatters
{
    internal class ConnectAckFormatter : Formatter<ConnectAck>
    {
        public override MqttPacketType PacketType { get { return MqttPacketType.ConnectAck; } }

        protected override ConnectAck Read( byte[] bytes )
        {
            ValidateHeaderFlag( bytes, t => t == MqttPacketType.ConnectAck, 0x00 );


            MqttProtocol.Encoding.DecodeRemainingLength( bytes, out int remainingLengthBytesLength );

            int connectAckFlagsIndex = MqttProtocol.PacketTypeLength + remainingLengthBytesLength;

            if( bytes.Byte( connectAckFlagsIndex ).Bits( 7 ) != 0x00 )
                throw new MqttException( Properties.ConnectAckFormatter_InvalidAckFlags );

            bool sessionPresent = bytes.Byte( connectAckFlagsIndex ).IsSet( 0 );
            MqttConnectionStatus returnCode = (MqttConnectionStatus)bytes.Byte( connectAckFlagsIndex + 1 );

            if( returnCode != MqttConnectionStatus.Accepted && sessionPresent )
                throw new MqttException( Properties.ConnectAckFormatter_InvalidSessionPresentForErrorReturnCode );

            ConnectAck connectAck = new ConnectAck( returnCode, sessionPresent );

            return connectAck;
        }

        protected override byte[] Write( ConnectAck packet )
        {
            byte[] variableHeader = GetVariableHeader( packet );
            byte[] remainingLength = MqttProtocol.Encoding.EncodeRemainingLength( variableHeader.Length );
            byte[] fixedHeader = GetFixedHeader( remainingLength );
            byte[] bytes = new byte[fixedHeader.Length + variableHeader.Length];

            fixedHeader.CopyTo( bytes, 0 );
            variableHeader.CopyTo( bytes, fixedHeader.Length );

            return bytes;
        }

        byte[] GetFixedHeader( byte[] remainingLength )
        {
            int flags = 0x00;
            int type = Convert.ToInt32( MqttPacketType.ConnectAck ) << 4;
            byte fixedHeaderByte1 = Convert.ToByte( flags | type );
            byte[] fixedHeader = new byte[remainingLength.Length + 1];

            fixedHeader[0] = fixedHeaderByte1;
            remainingLength.CopyTo( fixedHeader, 1 );

            return fixedHeader;
        }

        byte[] GetVariableHeader( ConnectAck packet )
        {
            if( packet.Status != MqttConnectionStatus.Accepted && packet.SessionPresent )
                throw new MqttException( Properties.ConnectAckFormatter_InvalidSessionPresentForErrorReturnCode );

            byte connectAckFlagsByte = Convert.ToByte( packet.SessionPresent );
            byte returnCodeByte = Convert.ToByte( packet.Status );

            return new[] { connectAckFlagsByte, returnCodeByte };
        }
    }
}
