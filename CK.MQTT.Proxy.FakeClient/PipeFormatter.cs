using CK.Core;
using Microsoft.IO;
using System;
using System.Buffers;
using System.Collections.Generic;
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

        public MessageFormatter( StubClientHeader stubClientHeader ) : this()
        {
            Bw.WriteEnum( stubClientHeader );
        }
        public MessageFormatter( RelayHeader relayHeader ) : this()
        {
            Bw.WriteEnum( relayHeader );
        }

        MessageFormatter()
        {
            _stream = _bufferManager.GetStream();
            Bw = new CKBinaryWriter( _stream );
        }

        public IMemoryOwner<byte> FormatMessage()
        {
            long previousPosition = _stream.Position;
            _stream.Position = 0;
            IMemoryOwner<byte> baseBuffer = MemoryPool<byte>.Shared.Rent( (int)_stream.Length );
            IMemoryOwner<byte> buffer = new CustomMemoryOwner<byte>( baseBuffer, (int)_stream.Length );
            Memory<byte> mem = buffer.Memory.Slice( 0, (int)_stream.Length );
            if( _stream.Read( mem.Span ) != _stream.Length ) throw new InvalidOperationException( "Didn't read the whole buffer." );//The spec say that a read may read less than asked.
            _stream.Position = previousPosition;
            return buffer;
        }

        class CustomMemoryOwner<T> : IMemoryOwner<T>
        {
            readonly IMemoryOwner<T> _memoryOwner;
            readonly int _newMemorySize;
            public CustomMemoryOwner( IMemoryOwner<T> memoryOwner, int newMemorySize )
            {
                _memoryOwner = memoryOwner;
                _newMemorySize = newMemorySize;
            }
            public Memory<T> Memory => _memoryOwner.Memory.Slice( 0, _newMemorySize );
            public void Dispose() => _memoryOwner.Dispose();
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

        public static byte[] ReadByteArray( this CKBinaryReader br ) => br.ReadBytes( br.ReadInt32() );

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
            !br.ReadBoolean() ?
                  null
                : new MqttLastWill(
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
            var clientId = br.ReadString();
            var userName = br.ReadNullableString();
            var password = br.ReadNullableString();
            return new MqttClientCredentials( clientId, userName, password );
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
            return new MqttApplicationMessage( br.ReadString(), br.ReadByteArray() );
        }

    }
    static class PipeExtensions
    {
        public static readonly RecyclableMemoryStreamManager _bufferManager = new RecyclableMemoryStreamManager();

        /// <summary>
        /// Copy asynchronously the next message in a Memory Stream.
        /// </summary>
        /// <returns>A memory stream containing the message.</returns>
        static async Task<CKBinaryReader> ReadMessageAsync( this PipeStream input, CancellationToken token )
        {
            MemoryStream stream = _bufferManager.GetStream();
            byte[] bufferArr = new byte[512];
            Memory<byte> buffer = new Memory<byte>( bufferArr );
            do
            {
                var readAmount = await input.ReadAsync( buffer, token );
                if( token.IsCancellationRequested ) return null;
                await stream.WriteAsync( buffer.Slice( 0, readAmount ) );
            } while( !input.IsMessageComplete );
            stream.Position = 0;
            return new CKBinaryReader( stream );
        }
        public static async Task<(RelayHeader header, CKBinaryReader reader>> ReadRelayMessage( this PipeStream input, CancellationToken token )
        {
            var reader = await ReadMessageAsync( input, token );
            if( reader.BaseStream.Length == 0 ) return (RelayHeader.EndOfStream, null);
            return (reader.ReadEnum<RelayHeader>(), reader);
        }

        public static async Task<(StubClientHeader header, CKBinaryReader reader>> ReadStubMessage( this PipeStream input, CancellationToken token )
        {
            var reader = await ReadMessageAsync( input, token );
            if( reader.BaseStream.Length == 0 ) return (StubClientHeader.EndOfStream, null);
            return (reader.ReadEnum<StubClientHeader>(), reader);
        }
    }
}
