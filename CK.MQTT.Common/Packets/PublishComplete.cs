using System;

namespace CK.MQTT.Common.Packets
{
    public class PublishComplete : IFlowPacket, IEquatable<PublishComplete>
    {
        public PublishComplete( ushort packetId )
        {
            PacketId = packetId;
        }

        public MqttPacketType Type => MqttPacketType.PublishComplete;

        public ushort PacketId { get; }

        public bool Equals( PublishComplete other )
        {
            if( other == null ) return false;
            return PacketId == other.PacketId;
        }

        public override bool Equals( object obj )
        {
            if( obj == null ) return false;
            if( !(obj is PublishComplete publishComplete) ) return false;
            return Equals( publishComplete );
        }

        public static bool operator ==( PublishComplete publishComplete, PublishComplete other )
        {
            if( publishComplete is null || other is null ) return Equals( publishComplete, other );
            return publishComplete.Equals( other );
        }

        public static bool operator !=( PublishComplete publishComplete, PublishComplete other )
        {
            if( publishComplete is null || other is null ) return !Equals( publishComplete, other );
            return !publishComplete.Equals( other );
        }

        public override int GetHashCode() => PacketId.GetHashCode();
    }
}
