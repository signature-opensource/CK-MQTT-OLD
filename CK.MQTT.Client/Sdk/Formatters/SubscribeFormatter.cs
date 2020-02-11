using CK.MQTT.Sdk.Packets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CK.MQTT.Sdk.Formatters
{
    internal class SubscribeFormatter : Formatter<Subscribe>
    {
        readonly IMqttTopicEvaluator topicEvaluator;

        public SubscribeFormatter( IMqttTopicEvaluator topicEvaluator )
        {
            this.topicEvaluator = topicEvaluator;
        }

        public override MqttPacketType PacketType { get { return MqttPacketType.Subscribe; } }

        protected override Subscribe Read( byte[] bytes )
        {
            ValidateHeaderFlag( bytes, t => t == MqttPacketType.Subscribe, 0x02 );

            int remainingLength = MqttProtocol.Encoding.DecodeRemainingLength( bytes, out int remainingLengthBytesLength );

            int packetIdentifierStartIndex = remainingLengthBytesLength + 1;
            ushort packetIdentifier = bytes.Bytes( packetIdentifierStartIndex, 2 ).ToUInt16();

            int headerLength = 1 + remainingLengthBytesLength + 2;
            IEnumerable<Subscription> subscriptions = GetSubscriptions( bytes, headerLength );

            return new Subscribe( packetIdentifier, subscriptions.ToArray() );
        }

        protected override byte[] Write( Subscribe packet )
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

            int flags = 0x02;
            int type = Convert.ToInt32( MqttPacketType.Subscribe ) << 4;

            byte fixedHeaderByte1 = Convert.ToByte( flags | type );

            fixedHeader.Add( fixedHeaderByte1 );
            fixedHeader.AddRange( remainingLength );

            return fixedHeader.ToArray();
        }

        byte[] GetVariableHeader( Subscribe packet )
        {
            List<byte> variableHeader = new List<byte>();

            byte[] packetIdBytes = MqttProtocol.Encoding.EncodeInteger( packet.PacketId );

            variableHeader.AddRange( packetIdBytes );

            return variableHeader.ToArray();
        }

        byte[] GetPayload( Subscribe packet )
        {
            if( packet.Subscriptions == null || !packet.Subscriptions.Any() )
                throw new MqttProtocolViolationException( ClientProperties.SubscribeFormatter_MissingTopicFilterQosPair );

            List<byte> payload = new List<byte>();

            foreach( Subscription subscription in packet.Subscriptions )
            {
                if( string.IsNullOrEmpty( subscription.TopicFilter ) )
                {
                    throw new MqttProtocolViolationException( ClientProperties.SubscribeFormatter_MissingTopicFilterQosPair );
                }

                if( !topicEvaluator.IsValidTopicFilter( subscription.TopicFilter ) )
                {
                    string error = string.Format( ClientProperties.SubscribeFormatter_InvalidTopicFilter, subscription.TopicFilter );

                    throw new MqttException( error );
                }

                byte[] topicBytes = MqttProtocol.Encoding.EncodeString( subscription.TopicFilter );
                byte requestedQosByte = Convert.ToByte( subscription.MaximumQualityOfService );

                payload.AddRange( topicBytes );
                payload.Add( requestedQosByte );
            }

            return payload.ToArray();
        }

        IEnumerable<Subscription> GetSubscriptions( byte[] bytes, int headerLength )
        {
            if( bytes.Length - headerLength < 4 ) //At least 4 bytes required on payload: MSB, LSB, Topic Filter, Requests QoS
                throw new MqttProtocolViolationException( ClientProperties.SubscribeFormatter_MissingTopicFilterQosPair );

            int index = headerLength;

            do
            {
                string topicFilter = bytes.GetString( index, out index );

                if( !topicEvaluator.IsValidTopicFilter( topicFilter ) )
                {
                    string error = string.Format( ClientProperties.SubscribeFormatter_InvalidTopicFilter, topicFilter );

                    throw new MqttException( error );
                }

                byte requestedQosByte = bytes.Byte( index );

                if( !Enum.IsDefined( typeof( MqttQualityOfService ), requestedQosByte ) )
                    throw new MqttProtocolViolationException( ClientProperties.Formatter_InvalidQualityOfService );

                MqttQualityOfService requestedQos = (MqttQualityOfService)requestedQosByte;

                yield return new Subscription( topicFilter, requestedQos );
                index++;
            } while( bytes.Length - index + 1 >= 2 );
        }
    }
}
