//using CK.Core;
//using System;
//using System.Buffers;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO.Pipes;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;

//namespace CK.MQTT.Proxy.FakeClient
//{
//    class RelayConnection : IDisposable
//    {
//        static readonly ITracer tracer = Tracer.Get<RelayConnection>();
//        readonly NamedPipeServerStream _namedPipe;
//        readonly MqttRelay _relay;
//        readonly IMqttClient _client;
//        readonly CancellationTokenSource _cancellationTokenSource;
//        Task _backgroundProcessing;

//        public RelayConnection( MqttRelay relay, IMqttClient client, string pipeName )
//        {
//            _namedPipe = new NamedPipeServerStream( pipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Message );
//            _relay = relay;
//            _client = client;
//            _cancellationTokenSource = new CancellationTokenSource();
//        }

//        public async ValueTask SendMessage( Memory<byte> message, CancellationToken token )
//        {
//            try
//            {
//                await _namedPipe.WriteAsync( message, token );
//            }
//            catch( Exception e )
//            {
//                _relay.Disconnect( this );
//                tracer.Critical( e, "Relay Connection dropped." );
//            }
//        }


//        public bool IsRunning => !_cancellationTokenSource.IsCancellationRequested;

//        public static async Task<RelayConnection> CreateRelayConnection( MqttRelay relay, IMqttClient client, string pipeName, CancellationToken cancellationToken )
//        {
//            var connection = new RelayConnection( relay, client, pipeName );
//            await connection._namedPipe.WaitForConnectionAsync( cancellationToken );
//            connection._backgroundProcessing = connection.ProcessClient( connection._cancellationTokenSource.Token );
//            return connection;
//        }

//        public void Dispose()
//        {
//            StopAsync().Wait( 100 );
//        }

//        public Task StopAsync()
//        {
//            _cancellationTokenSource.Cancel();
//            _namedPipe.Disconnect();
//            return _backgroundProcessing;
//        }

//        async Task ProcessClient( CancellationToken cancellationToken )
//        {
//            while( !cancellationToken.IsCancellationRequested && _namedPipe.IsConnected )
//            {
//                (StubClientHeader header, CKBinaryReader reader) = await _namedPipe.ReadStubMessage( cancellationToken );
//                using( reader )
//                {
//                    if( cancellationToken.IsCancellationRequested ) return;
//                    if( !_namedPipe.IsConnected ) return;
//                    switch( header )
//                    {
//                        case StubClientHeader.EndOfStream:
//                            return;
//                        case StubClientHeader.Disconnect:
//                            _relay.Disconnect( this );
//                            break;
//                        case StubClientHeader.ConnectAnonymous:
//                            using( var msg = new MessageFormatter( RelayHeader.ConnectResponse ) )
//                            {
//                                msg.Bw.WriteEnum( SessionState.CleanSession );
//                                using( IMemoryOwner<byte> memory = msg.FormatMessage() )
//                                {
//                                    await _namedPipe.WriteAsync( memory.Memory, cancellationToken );
//                                }
//                            }
//                            break;
//                        case StubClientHeader.Connect:
//                            using( var msg = new MessageFormatter( RelayHeader.ConnectResponse ) )
//                            {
//                                reader.ReadClientCredentials();
//                                reader.ReadLastWill();
//                                bool clean = reader.ReadBoolean();
//                                //await _client.ConnectAsync( br.ReadClientCredentials(), br.ReadLastWill(), br.ReadBoolean() )
//                                msg.Bw.WriteEnum( clean ? SessionState.CleanSession : SessionState.SessionPresent );//TODO
//                                using( IMemoryOwner<byte> memory = msg.FormatMessage() )
//                                {
//                                    await _namedPipe.WriteAsync( memory.Memory, cancellationToken );
//                                }
//                            }
//                            break;
//                        case StubClientHeader.Publish:
//                            await _client.PublishAsync( reader.ReadApplicationMessage(), reader.ReadEnum<MqttQualityOfService>(), reader.ReadBoolean() );
//                            break;
//                        case StubClientHeader.Subscribe:

//                            await _client.SubscribeAsync( reader.ReadString(), reader.ReadEnum<MqttQualityOfService>() );
//                            break;
//                        case StubClientHeader.Unsubscribe:
//                            string[] topics = new string[reader.Read()];
//                            for( int i = 0; i < topics.Length; i++ )
//                            {
//                                topics[i] = reader.ReadString();
//                            }
//                            await _client.UnsubscribeAsync( topics );
//                            break;
//                        case StubClientHeader.IsConnected:
//                            using( var msg = new MessageFormatter( RelayHeader.IsConnectedResponse ) )
//                            {
//                                msg.Bw.Write( _client.IsConnected );
//                                using( var returnMsg = msg.FormatMessage() )
//                                {
//                                    await _namedPipe.WriteAsync( returnMsg.Memory, cancellationToken );
//                                }
//                            }
//                            break;
//                        default:
//                            throw new InvalidOperationException( $"Unknown ClientHeader: {header.ToString()}." );
//                    }
//                }
//            }
//        }
//    }
//}
