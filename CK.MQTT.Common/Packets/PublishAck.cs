using System;

namespace CK.MQTT.Common.Packets
{
    public class PublishAck : IFlowPacket, IEquatable<PublishAck>
    {
        public PublishAck( ushort packetId )
        {
            PacketId = packetId;
        }

        public MqttPacketType Type => MqttPacketType.PublishAck;

        public ushort PacketId { get; }

        public bool Equals( PublishAck other )
        {
            if( other == null ) return false;

            return PacketId == other.PacketId;
        }

        public override bool Equals( object obj )
        {
            if( obj == null ) return false;
            if( !(obj is PublishAck publishAck) ) return false;
            return Equals( publishAck );
        }

        public static bool operator ==( PublishAck publishAck, PublishAck other )
        {
            if( publishAck is null || other is null ) return Equals( publishAck, other );
            return publishAck.Equals( other );
        }

        public static bool operator !=( PublishAck publishAck, PublishAck other )
        {
            if( publishAck is null || other is null ) return !Equals( publishAck, other );
            return !publishAck.Equals( other );
        }

        public override int GetHashCode() => PacketId.GetHashCode();
    }
}
