using CK.MQTT.Sdk.Packets;
using System;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk.Formatters
{
    internal abstract class Formatter<T> : IFormatter
        where T : class, IPacket
    {
        public abstract MqttPacketType PacketType { get; }

        protected abstract T Read( byte[] bytes );

        protected abstract byte[] Write( T packet );

        public Task<IPacket> FormatAsync( byte[] bytes )//TODO: make it synchronous.
        {
            MqttPacketType actualType = (MqttPacketType)bytes.Byte( 0 ).Bits( 4 );

            if( PacketType != actualType )
            {
                throw new MqttException( ClientProperties.Formatter_InvalidPacket( typeof( T ).Name ) );
            }
            return Task.FromResult<IPacket>( Read( bytes ) );
        }

        public Task<byte[]> FormatAsync( IPacket packet )//TODO: make it synchronous.
        {
            if( packet.Type != PacketType )
            {
                string error = ClientProperties.Formatter_InvalidPacket( typeof( T ).Name );

                throw new MqttException( error );
            }
            return Task.FromResult( Write( packet as T ) );
        }

        protected void ValidateHeaderFlag( byte[] bytes, Func<MqttPacketType, bool> packetTypePredicate, int expectedFlag )
        {
            byte headerFlag = bytes.Byte( 0 ).Bits( 5, 4 );

            if( packetTypePredicate( PacketType ) && headerFlag != expectedFlag )
            {
                string error = ClientProperties.Formatter_InvalidHeaderFlag( headerFlag, typeof( T ).Name, expectedFlag );

                throw new MqttException( error );
            }
        }
    }
}
