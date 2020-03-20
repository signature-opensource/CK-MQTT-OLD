using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace CK.MQTT.Sdk.Packets
{
    internal class Publish : IPacket, IEquatable<Publish>
    {
        public Publish( string topic, MqttQualityOfService qualityOfService, bool retain, bool duplicated, ushort? packetId = null )
        {
            QualityOfService = qualityOfService;
            Duplicated = duplicated;
            Retain = retain;
            Topic = topic;
            PacketId = packetId;
        }

        public MqttPacketType Type => MqttPacketType.Publish;

        public MqttQualityOfService QualityOfService { get; }

        public bool Duplicated { get; }

        public bool Retain { get; }

        public string Topic { get; }

        public ushort? PacketId { get; }

        public ReadOnlyMemory<byte> Payload { get; set; }

        public bool Equals( Publish other )
        {
            if( other == null )
                return false;

            bool equals = QualityOfService == other.QualityOfService &&
                Duplicated == other.Duplicated &&
                Retain == other.Retain &&
                Topic == other.Topic &&
                PacketId == other.PacketId;
            equals &= MemoryExtensions.SequenceEqual<byte>( Payload.Span, other.Payload.Span );

            return equals;
        }

        public override bool Equals( object obj )
        {
            if( obj == null ) return false;

            Publish publish = obj as Publish;

            if( publish == null ) return false;

            return Equals( publish );
        }

        public static bool operator ==( Publish publish, Publish other )
        {
            if( publish is null || other is null ) return Equals( publish, other );

            return publish.Equals( other );
        }

        public static bool operator !=( Publish publish, Publish other )
        {
            if( publish is null || other is null ) return !Equals( publish, other );

            return !publish.Equals( other );
        }

        public override int GetHashCode()
        {
            int hashCode = QualityOfService.GetHashCode() +
                Duplicated.GetHashCode() +
                Retain.GetHashCode() +
                Topic.GetHashCode();

            hashCode += Encoding.ASCII.GetString( Payload.Span ).GetHashCode();

            if( PacketId.HasValue )
            {
                hashCode += PacketId.Value.GetHashCode();
            }

            return hashCode;
        }
    }
}
