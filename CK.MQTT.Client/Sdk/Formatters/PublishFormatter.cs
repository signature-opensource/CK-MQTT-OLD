using CK.MQTT.Sdk.Packets;
using System;
using System.Collections.Generic;

namespace CK.MQTT.Sdk.Formatters
{
    internal class PublishFormatter : Formatter<Publish>
    {
        readonly IMqttTopicEvaluator _topicEvaluator;

        public PublishFormatter( IMqttTopicEvaluator topicEvaluator )
        {
            _topicEvaluator = topicEvaluator;
        }

        public override MqttPacketType PacketType { get { return MqttPacketType.Publish; } }

        protected override Publish Read( byte[] bytes )
        {
            int remainingLength = MqttProtocol.Encoding.DecodeRemainingLength( bytes, out int remainingLengthBytesLength );

            byte packetFlags = bytes.Byte( 0 ).Bits( 5, 4 );

            if( packetFlags.Bits( 6, 2 ) == 0x03 )
                throw new MqttException( ClientProperties.Formatter_InvalidQualityOfService );

            MqttQualityOfService qos = (MqttQualityOfService)packetFlags.Bits( 6, 2 );
            bool duplicated = packetFlags.IsSet( 3 );

            if( qos == MqttQualityOfService.AtMostOnce && duplicated )
                throw new MqttException( ClientProperties.PublishFormatter_InvalidDuplicatedWithQoSZero );

            bool retainFlag = packetFlags.IsSet( 0 );

            int topicStartIndex = 1 + remainingLengthBytesLength;
            string topic = bytes.GetString( topicStartIndex, out int nextIndex );

            if( !_topicEvaluator.IsValidTopicName( topic ) )
            {
                string error = string.Format( ClientProperties.PublishFormatter_InvalidTopicName, topic );
                throw new MqttException( error );
            }

            int variableHeaderLength = topic.Length + 2;
            ushort? packetId = default;

            if( qos != MqttQualityOfService.AtMostOnce )
            {
                packetId = bytes.Bytes( nextIndex, 2 ).ToUInt16();
                variableHeaderLength += 2;
            }

            Publish publish = new Publish( topic, qos, retainFlag, duplicated, packetId );

            if( remainingLength > variableHeaderLength )
            {
                int payloadStartIndex = 1 + remainingLengthBytesLength + variableHeaderLength;

                publish.Payload = bytes.Bytes( payloadStartIndex );
            }

            return publish;
        }

        protected override byte[] Write( Publish packet )
        {
            List<byte> bytes = new List<byte>();

            byte[] variableHeader = GetVariableHeader( packet );
            int payloadLength = packet.Payload == null ? 0 : packet.Payload.Length;
            byte[] remainingLength = MqttProtocol.Encoding.EncodeRemainingLength( variableHeader.Length + payloadLength );
            byte[] fixedHeader = GetFixedHeader( packet, remainingLength );

            bytes.AddRange( fixedHeader );
            bytes.AddRange( variableHeader );

            if( packet.Payload != null )
            {
                bytes.AddRange( packet.Payload );
            }

            return bytes.ToArray();
        }

        byte[] GetFixedHeader( Publish packet, byte[] remainingLength )
        {
            if( packet.QualityOfService == MqttQualityOfService.AtMostOnce && packet.Duplicated )
                throw new MqttException( ClientProperties.PublishFormatter_InvalidDuplicatedWithQoSZero );

            List<byte> fixedHeader = new List<byte>();

            int retain = Convert.ToInt32( packet.Retain );
            int qos = Convert.ToInt32( packet.QualityOfService );
            int duplicated = Convert.ToInt32( packet.Duplicated );

            qos <<= 1;
            duplicated <<= 3;

            byte flags = Convert.ToByte( retain | qos | duplicated );
            int type = Convert.ToInt32( MqttPacketType.Publish ) << 4;

            byte fixedHeaderByte1 = Convert.ToByte( flags | type );

            fixedHeader.Add( fixedHeaderByte1 );
            fixedHeader.AddRange( remainingLength );

            return fixedHeader.ToArray();
        }

        byte[] GetVariableHeader( Publish packet )
        {
            if( !_topicEvaluator.IsValidTopicName( packet.Topic ) )
                throw new MqttException( ClientProperties.PublishFormatter_InvalidTopicName );

            if( packet.PacketId.HasValue && packet.QualityOfService == MqttQualityOfService.AtMostOnce )
                throw new MqttException( ClientProperties.PublishFormatter_InvalidPacketId );

            if( !packet.PacketId.HasValue && packet.QualityOfService != MqttQualityOfService.AtMostOnce )
                throw new MqttException( ClientProperties.PublishFormatter_PacketIdRequired );

            List<byte> variableHeader = new List<byte>();

            byte[] topicBytes = MqttProtocol.Encoding.EncodeString( packet.Topic );

            variableHeader.AddRange( topicBytes );

            if( packet.PacketId.HasValue )
            {
                byte[] packetIdBytes = MqttProtocol.Encoding.EncodeInteger( packet.PacketId.Value );

                variableHeader.AddRange( packetIdBytes );
            }

            return variableHeader.ToArray();
        }
    }
}
