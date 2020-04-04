using System;
using System.Collections.Generic;
using System.Linq;

namespace CK.MQTT.Common.Packets
{
    internal class Subscribe : IFlowPacket, IEquatable<Subscribe>
    {
        public Subscribe( ushort packetId, params Subscription[] subscriptions )
        {
            PacketId = packetId;
            Subscriptions = subscriptions;
        }

        public MqttPacketType Type => MqttPacketType.Subscribe;

        public ushort PacketId { get; }

        public IEnumerable<Subscription> Subscriptions { get; }

        public bool Equals( Subscribe other )
        {
            if( other == null ) return false;
            return PacketId == other.PacketId &&
                Subscriptions.SequenceEqual( other.Subscriptions );
        }

        public override bool Equals( object obj )
        {
            if( obj == null ) return false;

            if( !(obj is Subscribe subscribe) ) return false;
            return Equals( subscribe );
        }

        public static bool operator ==( Subscribe subscribe, Subscribe other )
        {
            if( subscribe is null || other is null ) return Equals( subscribe, other );
            return subscribe.Equals( other );
        }

        public static bool operator !=( Subscribe subscribe, Subscribe other )
        {
            if( subscribe is null || other is null ) return !Equals( subscribe, other );
            return !subscribe.Equals( other );
        }

        public override int GetHashCode() => PacketId.GetHashCode() + Subscriptions.GetHashCode();
    }
}
