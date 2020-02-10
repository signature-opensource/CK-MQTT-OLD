using System;

namespace CK.MQTT.Sdk.Packets
{
    internal class UnsubscribeAck : IFlowPacket, IEquatable<UnsubscribeAck>
    {
        public UnsubscribeAck( ushort packetId )
        {
            PacketId = packetId;
        }

        public MqttPacketType Type => MqttPacketType.UnsubscribeAck;

        public ushort PacketId { get; }

        public bool Equals( UnsubscribeAck other )
        {
            if( other == null ) return false;
            return PacketId == other.PacketId;
        }

        public override bool Equals( object obj )
        {
            if( obj == null ) return false;
            if( !(obj is UnsubscribeAck unsubscribeAck)) return false;

            return Equals( unsubscribeAck );
        }

        public static bool operator ==( UnsubscribeAck unsubscribeAck, UnsubscribeAck other )
        {
            if( unsubscribeAck is null || other is null ) return Equals( unsubscribeAck, other );

            return unsubscribeAck.Equals( other );
        }

        public static bool operator !=( UnsubscribeAck unsubscribeAck, UnsubscribeAck other )
        {
            if( unsubscribeAck is null || other is null ) return !Equals( unsubscribeAck, other );
            return !unsubscribeAck.Equals( other );
        }

        public override int GetHashCode() => PacketId.GetHashCode();
    }
}
