using System;

namespace CK.MQTT.Sdk.Packets
{
    internal class ConnectAck : IPacket, IEquatable<ConnectAck>
    {
        public ConnectAck( MqttConnectionStatus status, bool existingSession )
        {
            Status = status;
            SessionPresent = existingSession;
        }

        public MqttPacketType Type { get { return MqttPacketType.ConnectAck; } }

        public MqttConnectionStatus Status { get; }

        public bool SessionPresent { get; }

        public bool Equals( ConnectAck other )
        {
            if( other == null ) return false;

            return Status == other.Status &&
                SessionPresent == other.SessionPresent;
        }

        public override bool Equals( object obj )
        {
            if( obj == null )
                return false;

            ConnectAck connectAck = obj as ConnectAck;

            if( connectAck == null )
                return false;

            return Equals( connectAck );
        }

        public static bool operator ==( ConnectAck connectAck, ConnectAck other )
        {
            if( connectAck is null || other is null )
                return Equals( connectAck, other );

            return connectAck.Equals( other );
        }

        public static bool operator !=( ConnectAck connectAck, ConnectAck other )
        {
            if( connectAck is null || other is null )
                return !Equals( connectAck, other );

            return !connectAck.Equals( other );
        }

        public override int GetHashCode()
        {
            return Status.GetHashCode() + SessionPresent.GetHashCode();
        }
    }
}
