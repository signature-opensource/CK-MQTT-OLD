using CK.MQTT.Packets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CK.MQTT.Sdk.Packets
{
    internal class SubscribeAck : IPacket, IEquatable<SubscribeAck>
    {
        public SubscribeAck( ushort packetId, params SubscribeReturnCode[] returnCodes )
        {
            PacketId = packetId;
            ReturnCodes = returnCodes;
        }

        public MqttPacketType Type => MqttPacketType.SubscribeAck;

        public ushort PacketId { get; }

        public SubscribeReturnCode[] ReturnCodes { get; }

        public bool Equals( SubscribeAck other )
        {
            if( other == null ) return false;

            return PacketId == other.PacketId &&
                ReturnCodes.SequenceEqual( other.ReturnCodes );
        }

        public override bool Equals( object obj )
        {
            if( obj == null ) return false;
            if( !(obj is SubscribeAck subscribeAck) ) return false;

            return Equals( subscribeAck );
        }

        public static bool operator ==( SubscribeAck subscribeAck, SubscribeAck other )
        {
            if( subscribeAck is null || other is null ) return Equals( subscribeAck, other );

            return subscribeAck.Equals( other );
        }

        public static bool operator !=( SubscribeAck subscribeAck, SubscribeAck other )
        {
            if( subscribeAck is null || other is null )
                return !Equals( subscribeAck, other );

            return !subscribeAck.Equals( other );
        }

        public override int GetHashCode() => PacketId.GetHashCode() + ReturnCodes.GetHashCode();
    }
}
