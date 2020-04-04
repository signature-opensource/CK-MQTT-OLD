using System;

namespace CK.MQTT.Common.Packets
{
    internal class PublishRelease : IFlowPacket, IEquatable<PublishRelease>
    {
        public PublishRelease( ushort packetId )
        {
            PacketId = packetId;
        }

        public MqttPacketType Type => MqttPacketType.PublishRelease;

        public ushort PacketId { get; }

        public bool Equals( PublishRelease other )
        {
            if( other == null ) return false;

            return PacketId == other.PacketId;
        }

        public override bool Equals( object obj )
        {
            if( obj == null ) return false;
            if( !(obj is PublishRelease publishRelease) ) return false;
            return Equals( publishRelease );
        }

        public static bool operator ==( PublishRelease publishRelease, PublishRelease other )
        {
            if( publishRelease is null || other is null ) return Equals( publishRelease, other );
            return publishRelease.Equals( other );
        }

        public static bool operator !=( PublishRelease publishRelease, PublishRelease other )
        {
            if( publishRelease is null || other is null ) return !Equals( publishRelease, other );
            return !publishRelease.Equals( other );
        }

        public override int GetHashCode() => PacketId.GetHashCode();
    }
}
