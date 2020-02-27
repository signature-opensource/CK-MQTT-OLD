using CK.MQTT.Sdk.Packets;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CK.MQTT.Sdk.Formatters
{
    internal class ConnectFormatter : Formatter<Connect>
    {
        public override MqttPacketType PacketType => MqttPacketType.Connect;

        protected override Connect Read( byte[] bytes )
        {
            ValidateHeaderFlag( bytes, t => t == MqttPacketType.Connect, 0x00 );


            MqttProtocol.Encoding.DecodeRemainingLength( bytes, out int remainingLengthBytesLength );

            string protocolName = bytes.GetString( MqttProtocol.PacketTypeLength + remainingLengthBytesLength );

            if( protocolName != MqttProtocol.Name )
            {

                throw new MqttException( ClientProperties.ConnectFormatter_InvalidProtocolName( protocolName ) );
            }

            int protocolLevelIndex = MqttProtocol.PacketTypeLength + remainingLengthBytesLength + MqttProtocol.NameLength;
            byte protocolLevel = bytes.Byte( protocolLevelIndex );

            if( protocolLevel < MqttProtocol.SupportedLevel )
            {

                throw new MqttConnectionException(
                    MqttConnectionStatus.UnacceptableProtocolVersion,
                    ClientProperties.ConnectFormatter_UnsupportedLevel( protocolLevel )
                );
            }

            int protocolLevelLength = 1;
            int connectFlagsIndex = protocolLevelIndex + protocolLevelLength;
            byte connectFlags = bytes.Byte( connectFlagsIndex );

            if( connectFlags.IsSet( 0 ) )
                throw new MqttException( ClientProperties.ConnectFormatter_InvalidReservedFlag );

            if( connectFlags.Bits( 4, 2 ) == 0x03 )
                throw new MqttException( ClientProperties.Formatter_InvalidQualityOfService );

            bool willFlag = connectFlags.IsSet( 2 );
            bool willRetain = connectFlags.IsSet( 5 );

            if( !willFlag && willRetain )
                throw new MqttException( ClientProperties.ConnectFormatter_InvalidWillRetainFlag );

            bool userNameFlag = connectFlags.IsSet( 7 );
            bool passwordFlag = connectFlags.IsSet( 6 );

            if( !userNameFlag && passwordFlag )
                throw new MqttException( ClientProperties.ConnectFormatter_InvalidPasswordFlag );

            MqttQualityOfService willQos = (MqttQualityOfService)connectFlags.Bits( 4, 2 );
            bool cleanSession = connectFlags.IsSet( 1 );

            int keepAliveLength = 2;
            byte[] keepAliveBytes = bytes.Bytes( connectFlagsIndex + 1, keepAliveLength );
            ushort keepAlive = keepAliveBytes.ToUInt16();

            int payloadStartIndex = connectFlagsIndex + keepAliveLength + 1;
            string clientId = bytes.GetString( payloadStartIndex, out int nextIndex );

            if( clientId.Length > MqttProtocol.ClientIdMaxLength )
                throw new MqttConnectionException( MqttConnectionStatus.IdentifierRejected, ClientProperties.ConnectFormatter_ClientIdMaxLengthExceeded );

            if( !IsValidClientId( clientId ) )
            {
                string error = ClientProperties.ConnectFormatter_InvalidClientIdFormat( clientId );

                throw new MqttConnectionException( MqttConnectionStatus.IdentifierRejected, error );
            }

            if( string.IsNullOrEmpty( clientId ) && !cleanSession )
                throw new MqttConnectionException( MqttConnectionStatus.IdentifierRejected, ClientProperties.ConnectFormatter_ClientIdEmptyRequiresCleanSession );

            if( string.IsNullOrEmpty( clientId ) )
            {
                clientId = MqttClient.GetAnonymousClientId();
            }

            Connect connect = new Connect( clientId, cleanSession, protocolLevel )
            {
                KeepAlive = keepAlive
            };

            if( willFlag )
            {
                string willTopic = bytes.GetString( nextIndex, out int willMessageIndex );
                byte[] willMessageLengthBytes = bytes.Bytes( willMessageIndex, count: 2 );
                ushort willMessageLenght = willMessageLengthBytes.ToUInt16();

                byte[] willMessage = bytes.Bytes( willMessageIndex + 2, willMessageLenght );

                connect.Will = new MqttLastWill( willTopic, willQos, willRetain, willMessage );
                nextIndex = willMessageIndex + 2 + willMessageLenght;
            }

            if( userNameFlag )
            {
                string userName = bytes.GetString( nextIndex, out nextIndex );

                connect.UserName = userName;
            }

            if( passwordFlag )
            {
                string password = bytes.GetString( nextIndex );

                connect.Password = password;
            }

            return connect;
        }

        protected override byte[] Write( Connect packet )
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
            int type = Convert.ToInt32( MqttPacketType.Connect ) << 4;

            byte fixedHeaderByte1 = Convert.ToByte( flags | type );

            fixedHeader.Add( fixedHeaderByte1 );
            fixedHeader.AddRange( remainingLength );

            return fixedHeader.ToArray();
        }

        byte[] GetVariableHeader( Connect packet )
        {
            List<byte> variableHeader = new List<byte>();

            byte[] protocolNameBytes = MqttProtocol.Encoding.EncodeString( MqttProtocol.Name );
            byte protocolLevelByte = Convert.ToByte( MqttProtocol.SupportedLevel );

            int reserved = 0x00;
            int cleanSession = Convert.ToInt32( packet.CleanSession );
            int willFlag = Convert.ToInt32( packet.Will != null );
            int willQos = packet.Will == null ? 0 : Convert.ToInt32( packet.Will.QualityOfService );
            int willRetain = packet.Will == null ? 0 : Convert.ToInt32( packet.Will.Retain );
            int userNameFlag = Convert.ToInt32( !string.IsNullOrEmpty( packet.UserName ) );
            int passwordFlag = userNameFlag == 1 ? Convert.ToInt32( !string.IsNullOrEmpty( packet.Password ) ) : 0;

            if( userNameFlag == 0 && passwordFlag == 1 )
                throw new MqttException( ClientProperties.ConnectFormatter_InvalidPasswordFlag );

            cleanSession <<= 1;
            willFlag <<= 2;
            willQos <<= 3;
            willRetain <<= 5;
            passwordFlag <<= 6;
            userNameFlag <<= 7;

            byte connectFlagsByte = Convert.ToByte( reserved | cleanSession | willFlag | willQos | willRetain | passwordFlag | userNameFlag );
            byte[] keepAliveBytes = MqttProtocol.Encoding.EncodeInteger( packet.KeepAlive );

            variableHeader.AddRange( protocolNameBytes );
            variableHeader.Add( protocolLevelByte );
            variableHeader.Add( connectFlagsByte );
            variableHeader.Add( keepAliveBytes[keepAliveBytes.Length - 2] );
            variableHeader.Add( keepAliveBytes[keepAliveBytes.Length - 1] );

            return variableHeader.ToArray();
        }

        byte[] GetPayload( Connect packet )
        {
            if( packet.ClientId.Length > MqttProtocol.ClientIdMaxLength )
                throw new MqttException( ClientProperties.ConnectFormatter_ClientIdMaxLengthExceeded );

            if( !IsValidClientId( packet.ClientId ) )
            {
                throw new MqttException( ClientProperties.ConnectFormatter_InvalidClientIdFormat( packet.ClientId ) );
            }

            List<byte> payload = new List<byte>();

            byte[] clientIdBytes = MqttProtocol.Encoding.EncodeString( packet.ClientId );

            payload.AddRange( clientIdBytes );

            if( packet.Will != null )
            {
                byte[] willTopicBytes = MqttProtocol.Encoding.EncodeString( packet.Will.Topic );
                byte[] willMessageBytes = packet.Will.Payload;
                byte[] willMessageLengthBytes = MqttProtocol.Encoding.EncodeInteger( willMessageBytes.Length );

                payload.AddRange( willTopicBytes );
                payload.Add( willMessageLengthBytes[willMessageLengthBytes.Length - 2] );
                payload.Add( willMessageLengthBytes[willMessageLengthBytes.Length - 1] );
                payload.AddRange( willMessageBytes );
            }

            if( string.IsNullOrEmpty( packet.UserName ) && !string.IsNullOrEmpty( packet.Password ) )
                throw new MqttException( ClientProperties.ConnectFormatter_PasswordNotAllowed );

            if( !string.IsNullOrEmpty( packet.UserName ) )
            {
                byte[] userNameBytes = MqttProtocol.Encoding.EncodeString( packet.UserName );

                payload.AddRange( userNameBytes );
            }

            if( !string.IsNullOrEmpty( packet.Password ) )
            {
                byte[] passwordBytes = MqttProtocol.Encoding.EncodeString( packet.Password );

                payload.AddRange( passwordBytes );
            }

            return payload.ToArray();
        }

        bool IsValidClientId( string clientId )
        {
		    return true;
        }
    }
}
