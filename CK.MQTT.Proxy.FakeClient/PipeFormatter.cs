using CK.Core;
using Microsoft.IO;
using System;
using System.Buffers;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace CK.MQTT.Proxy.FakeClient
{
    class MessageFormatter : IDisposable
    {
        public static readonly RecyclableMemoryStreamManager _bufferManager = new RecyclableMemoryStreamManager();

        readonly Stream _stream;

        public CKBinaryWriter Bw { get; }

        public MessageFormatter()
        {
            _stream = _bufferManager.GetStream();
            Bw = new CKBinaryWriter( _stream );
        }

        public async Task SendMessageAsync( PipeStream outStream )
        {
            _stream.Position = 0;
            using( var buffer = MemoryPool<byte>.Shared.Rent( (int)_stream.Length ) )
            {
                var mem = buffer.Memory.Slice( 0, (int)_stream.Length );
                if( _stream.Read( mem.Span ) != _stream.Length ) throw new InvalidOperationException( "Didn't read the whole buffer." );//The spec say that a read may read less than asked.
                await outStream.WriteAsync( mem );
            }
        }

        public void Dispose()
        {
            _stream.Dispose();
        }
    }

    static class BinaryExtensions
    {
        public static void WriteByteArray( this CKBinaryWriter bw, byte[] arr )
        {
            bw.Write( arr.Length );
            bw.Write( arr );
        }

        public static byte[] ReadByteArray( this CKBinaryReader br ) => br.ReadBytes( br.Read() );

        public static void Write( this CKBinaryWriter bw, MqttLastWill will )
        {
            bw.Write( will != null );
            if( will == null ) return;
            bw.Write( will.Topic != null );
            if( will.Topic != null ) bw.Write( will.Topic );
            bw.WriteEnum( will.QualityOfService );
            bw.Write( will.Retain );
            bw.WriteByteArray( will.Payload );
        }

        public static MqttLastWill ReadLastWill( this CKBinaryReader br ) =>
            !br.ReadBoolean() ? null :
            new MqttLastWill(
                br.ReadBoolean() ? br.ReadString() : null,
                br.ReadEnum<MqttQualityOfService>(),
                br.ReadBoolean(),
                br.ReadByteArray() );

        public static void Write( this CKBinaryWriter bw, MqttClientCredentials creds )
        {
            bw.Write( creds.ClientId );
            bw.WriteNullableString( creds.UserName );
            bw.WriteNullableString( creds.Password );
        }

        public static MqttClientCredentials ReadClientCredentials( this CKBinaryReader br )
        {
            return new MqttClientCredentials( br.ReadString(), br.ReadNullableString(), br.ReadNullableString() );
        }

        public static void Write( this CKBinaryWriter bw, MqttEndpointDisconnected disconnected )
        {
            bw.WriteEnum( disconnected.Reason );
            bw.WriteNullableString( disconnected.Message );
        }

        public static MqttEndpointDisconnected ReadDisconnectEvent( this CKBinaryReader br )
        {
            return new MqttEndpointDisconnected( br.ReadEnum<DisconnectedReason>(), br.ReadNullableString() );
        }

        public static void Write( this CKBinaryWriter bw, MqttApplicationMessage msg )
        {
            bw.Write( msg.Topic );
            bw.Write( msg.Payload.Length );
            bw.Write( msg.Payload );
        }

        public static MqttApplicationMessage ReadApplicationMessage( this CKBinaryReader br )
        {
            return new MqttApplicationMessage( br.ReadString(), br.ReadBytes( br.Read() ) );
        }

    }
    static class PipeExtensions
    {
        public static readonly RecyclableMemoryStreamManager _bufferManager = new RecyclableMemoryStreamManager();

        /// <summary>
        /// Copy asynchronously the next message in a Memory Stream.
        /// </summary>
        /// <returns>A memory stream containing the message.</returns>
        public static async Task<MemoryStream> ReadMessageAsync( this PipeStream input, CancellationToken token )
        {
            MemoryStream stream = _bufferManager.GetStream();
            byte[] bufferArr = new byte[512];
            Memory<byte> buffer = new Memory<byte>( bufferArr );
            do
            {
                var readAmount = await input.ReadAsync( buffer, token );
                await stream.WriteAsync( buffer.Slice( 0, readAmount ) );
                if( token.IsCancellationRequested ) return null;
            } while( !input.IsMessageComplete );
            stream.Position = 0;
            return stream;
        }
    }
}
