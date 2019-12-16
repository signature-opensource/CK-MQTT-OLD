using Microsoft.IO;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;

namespace CK.MQTT.Proxy.FakeClient
{
    class PipeFormatter
    {
        readonly PipeStream _pipe;
        readonly BinaryFormatter _bf;
        readonly RecyclableMemoryStreamManager _bufferManager;

        public PipeFormatter( PipeStream pipeStream )
        {
            _bf = new BinaryFormatter();
            _bufferManager = new RecyclableMemoryStreamManager();
            _pipe = pipeStream;
        }
        public async Task SendPayloadAsync( params object[] objs )
        {
            using( MemoryStream ms = _bufferManager.GetStream() ) // We need to send this in a single message; ie: in a single write.
            {
                for( int i = 0; i < objs.Length; i++ )
                {
                    _bf.Serialize( ms, objs[i] );
                }
                if( ms.Position > int.MaxValue ) throw new ArgumentOutOfRangeException( "The serialized object is too big." );
                ms.Position = 0;
                using( var buffer = MemoryPool<byte>.Shared.Rent( (int)ms.Length ) )
                {
                    var memory = buffer.Memory;
                    do
                    {
                        memory = memory.Slice( await ms.ReadAsync( memory ) );

                    } while( memory.Length > 0 );

                    await _pipe.WriteAsync( buffer.Memory );
                }
            }
        }

        public void SendPayload( params object[] objs )
        {
            using( MemoryStream ms = _bufferManager.GetStream() ) // We need to send this in a single message; ie: in a single write.
            {
                for( int i = 0; i < objs.Length; i++ )
                {
                    _bf.Serialize( ms, objs[i] );
                }
                if( ms.Position > int.MaxValue ) throw new ArgumentOutOfRangeException( "The serialized object is too big." );
                ms.Position = 0;
                using( var buffer = MemoryPool<byte>.Shared.Rent( (int)ms.Length ) )
                {
                    var memory = buffer.Memory;
                    do
                    {
                        memory = memory.Slice( ms.Read( memory.Span ) );

                    } while( memory.Length > 0 );

                    _pipe.Write( buffer.Memory.Span );
                }
            }
        }

        public Stack ReceivePayload()
        {
            Stack stack = new Stack();
            do
            {
                stack.Push( ReceiveObject() );
            } while( !_pipe.IsMessageComplete );
            return stack;
        }

        public async Task<Queue<object>> ReceivePayloadAsync( CancellationToken token )
        {
            using( MemoryStream stream = _bufferManager.GetStream() )
            {
                byte[] bufferArr = new byte[512];
                Memory<byte> buffer = new Memory<byte>( bufferArr );
                do
                {
                    await _pipe.ReadAsync( buffer, token );
                    await stream.WriteAsync( buffer );
                    if( token.IsCancellationRequested ) return null;
                } while( !_pipe.IsMessageComplete );
                stream.Position = 0;
                Queue<object> stack = new Queue<object>();
                while( stream.Position != stream.Length )
                {
                    stack.Enqueue( _bf.Deserialize( stream ) );
                }
                return stack;
            }
        }

        public object ReceiveObject()
        {
            return _bf.Deserialize( _pipe );
        }
    }
}
