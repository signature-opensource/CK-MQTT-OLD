using System;

namespace CK.MQTT.Common.Packets
{
    public class PublishReceived : IFlowPacket, IEquatable<PublishReceived>
    {
        public PublishReceived( ushort packetId )
        {
            PacketId = packetId;
        }

        public MqttPacketType Type => MqttPacketType.PublishReceived;

        public ushort PacketId { get; }

        public bool Equals( PublishReceived other )
        {
            return other == null ? false : PacketId == other.PacketId;
        }

        public override bool Equals( object obj )
        {
            if( obj == null ) return false;
            if( !(obj is PublishReceived publishReceived) ) return false;

            return Equals( publishReceived );
        }

        public static bool operator ==( PublishReceived publishReceived, PublishReceived other )
        {
            if( publishReceived is null || other is null ) return Equals( publishReceived, other );

            return publishReceived.Equals( other );
        }

        public static bool operator !=( PublishReceived publishReceived, PublishReceived other )
        {
            if( publishReceived is null || other is null ) return !Equals( publishReceived, other );

            return !publishReceived.Equals( other );
        }

        public override int GetHashCode() => PacketId.GetHashCode();
    }
}
