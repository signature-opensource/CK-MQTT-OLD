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

        public async Task<IPacket> FormatAsync( byte[] bytes )
        {
            MqttPacketType actualType = (MqttPacketType)bytes.Byte( 0 ).Bits( 4 );

            if( PacketType != actualType )
            {
                string error = string.Format( Properties.Resources.GetString( "Formatter_InvalidPacket" ), typeof( T ).Name );

                throw new MqttException( error );
            }

            T packet = await Task.Run( () => Read( bytes ) )
                .ConfigureAwait( continueOnCapturedContext: false );

            return packet;
        }

        public async Task<byte[]> FormatAsync( IPacket packet )
        {
            if( packet.Type != PacketType )
            {
                string error = string.Format( Properties.Resources.GetString( "Formatter_InvalidPacket" ), typeof( T ).Name );

                throw new MqttException( error );
            }

            byte[] bytes = await Task.Run( () => Write( packet as T ) )
                .ConfigureAwait( continueOnCapturedContext: false );

            return bytes;
        }

        protected void ValidateHeaderFlag( byte[] bytes, Func<MqttPacketType, bool> packetTypePredicate, int expectedFlag )
        {
            byte headerFlag = bytes.Byte( 0 ).Bits( 5, 4 );

            if( packetTypePredicate( PacketType ) && headerFlag != expectedFlag )
            {
                string error = string.Format( Properties.Resources.GetString( "Formatter_InvalidHeaderFlag" ), headerFlag, typeof( T ).Name, expectedFlag );

                throw new MqttException( error );
            }
        }
    }
}
