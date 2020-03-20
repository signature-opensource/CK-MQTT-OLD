using System;
using System.Collections.Generic;
using System.Linq;

namespace CK.MQTT.Sdk.Packets
{
    internal class Unsubscribe : IPacket, IEquatable<Unsubscribe>
    {
        public Unsubscribe( ushort packetId, IEnumerable<string> topics )
        {
            PacketId = packetId;
            Topics = topics;
        }

        public MqttPacketType Type => MqttPacketType.Unsubscribe;

        public ushort PacketId { get; }

        public IEnumerable<string> Topics { get; }

        public bool Equals( Unsubscribe other )
        {
            if( other == null ) return false;

            return PacketId == other.PacketId &&
                Topics.SequenceEqual( other.Topics );
        }

        public override bool Equals( object obj )
        {
            if( obj == null ) return false;
            if( !(obj is Unsubscribe unsubscribe) ) return false;

            return Equals( unsubscribe );
        }

        public static bool operator ==( Unsubscribe unsubscribe, Unsubscribe other )
        {
            if( unsubscribe is null || other is null ) return Equals( unsubscribe, other );
            return unsubscribe.Equals( other );
        }

        public static bool operator !=( Unsubscribe unsubscribe, Unsubscribe other )
        {
            if( unsubscribe is null || other is null ) return !Equals( unsubscribe, other );
            return !unsubscribe.Equals( other );
        }

        public override int GetHashCode() => PacketId.GetHashCode() + Topics.GetHashCode();
    }
}
