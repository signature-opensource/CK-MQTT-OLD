using System;

namespace CK.MQTT.Common.Packets
{
    internal class ConnectAck : IPacket, IEquatable<ConnectAck>
    {
        public ConnectAck( ConnectionStatus status, SessionState existingSession )
        {
            Status = status;
            SessionState = existingSession;
        }

        public MqttPacketType Type { get { return MqttPacketType.ConnectAck; } }

        public ConnectionStatus Status { get; }

        public SessionState SessionState { get; }

        public bool Equals( ConnectAck other )
            => Status == other.Status && SessionState == other.SessionState;

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
            return Status.GetHashCode() + SessionState.GetHashCode();
        }
    }
}
