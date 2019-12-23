using CK.Core;
using System;
using System.Collections;
using System.IO;
using System.IO.Pipes;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace CK.MQTT.Proxy.FakeClient
{
    public class MqttStubClient : IMqttClient
    {
        readonly IActivityMonitor _m;
        private readonly int _waitTimeoutSecs;
        readonly NamedPipeClientStream _pipe;
        CancellationTokenSource _listenerCancel;
        Task _listener;
        readonly ReplaySubject<MqttApplicationMessage> _receiver;
        MqttStubClient( IActivityMonitor m, int waitTimeoutSecs, NamedPipeClientStream namedPipeClientStream )
        {
            _m = m;
            _waitTimeoutSecs = waitTimeoutSecs;
            _pipe = namedPipeClientStream;
            _receiver = new ReplaySubject<MqttApplicationMessage>();
        }

        public static MqttStubClient Create( IActivityMonitor m, int waitTimeoutSecs, string pipeName = "ck_mqtt", string serverName = "." )
        {
            var pipe = new NamedPipeClientStream( serverName, pipeName, PipeDirection.InOut, PipeOptions.Asynchronous );
            return new MqttStubClient( m, waitTimeoutSecs, pipe );
        }

        public async Task BackgroundListening( CancellationToken token )
        {
            while( !token.IsCancellationRequested )
            {
                (RelayHeader header, CKBinaryReader reader) = await _pipe.ReadRelayMessage( token );
                using( reader )
                {
                    switch( reader.ReadEnum<RelayHeader>() )
                    {
                        case RelayHeader.Disconnected:
                            Disconnected?.Invoke( this, reader.ReadDisconnectEvent() );
                            break;
                        case RelayHeader.MessageEvent:
                            _receiver.OnNext( reader.ReadApplicationMessage() );
                            break;
                        case RelayHeader.EndOfStream:
                            return;
                        default:
                            throw new InvalidDataException( "Unknown Relay Header." );
                    }
                    if( reader.BaseStream.Length != reader.BaseStream.Position ) throw new DataMisalignedException( "Expected to read the entierty of the stream." );
                }
            }
        }

        public string Id => null;

        public bool IsConnected => _pipe.IsConnected;

        public IObservable<MqttApplicationMessage> MessageStream => _receiver;

        public event EventHandler<MqttEndpointDisconnected> Disconnected;

        async Task<SessionState> ConnectAsyncInternal( MqttClientCredentials credentials = null, MqttLastWill will = null, bool cleanSession = false )
        {
            _listenerCancel = new CancellationTokenSource();
            await _pipe.ConnectAsync( _waitTimeoutSecs * 100000 );
            _pipe.ReadMode = PipeTransmissionMode.Message;
            if( credentials == null )
            {
                using( MessageFormatter mf = new MessageFormatter( StubClientHeader.ConnectAnonymous ) )//anonymous
                {
                    mf.Bw.Write( will );
                    await mf.SendMessageAsync( _pipe ); //I would like async disposable :'(
                }
            }
            else
            {
                using( MessageFormatter mf = new MessageFormatter( StubClientHeader.Connect ) )
                {
                    mf.Bw.Write( credentials );
                    mf.Bw.Write( will );
                    mf.Bw.Write( cleanSession );
                    await mf.SendMessageAsync( _pipe );
                }
            }
            (RelayHeader header, CKBinaryReader reader) = await _pipe.ReadRelayMessage( CancellationToken.None );
            if( header != RelayHeader.ConnectResponse ) throw new InvalidOperationException( "Excpected connect response header." );
            using( reader )
            {
                SessionState state = reader.ReadEnum<SessionState>();
                if( reader.BaseStream.Position != reader.BaseStream.Length ) throw new DataMisalignedException( "More data to read than expected." );
                _listener = BackgroundListening( _listenerCancel.Token );
                return state;
            }
        }

        public Task<SessionState> ConnectAsync( MqttClientCredentials credentials, MqttLastWill will = null, bool cleanSession = false )
        {
            if( credentials == null ) throw new ArgumentNullException( "credentials are null, use the method with MqttClientCredentials if you want to do anonymous connection." );
            return ConnectAsyncInternal( credentials, will, cleanSession );
        }

        public Task<SessionState> ConnectAsync( MqttLastWill will = null )
        {
            return ConnectAsyncInternal( will: will );
        }

        public async Task DisconnectAsync()
        {
            _receiver.OnCompleted();
        }

        public void Dispose()
        {
            _listenerCancel?.Cancel();
            try
            {
                _listener?.Wait();
            }
            catch( AggregateException e )
            {
                if( e.InnerException.GetType() != typeof( TaskCanceledException ) ) throw;
            }
            _pipe.Dispose();
            _receiver.Dispose();
        }

        public Task PublishAsync( MqttApplicationMessage message, MqttQualityOfService qos, bool retain = false )
        {
            using( var msg = new MessageFormatter( StubClientHeader.Publish ) )
            {
                msg.Bw.Write( message );
                msg.Bw.WriteEnum( qos );
                msg.Bw.Write( retain );
                return msg.SendMessageAsync( _pipe );
            }
        }

        public Task SubscribeAsync( string topicFilter, MqttQualityOfService qos )
        {
            using( var msg = new MessageFormatter( StubClientHeader.Subscribe ) )
            {
                msg.Bw.Write( topicFilter );
                msg.Bw.WriteEnum( qos );
                return msg.SendMessageAsync( _pipe );
            }
        }

        public Task UnsubscribeAsync( params string[] topics )
        {
            using( var msg = new MessageFormatter( StubClientHeader.Unsubscribe ) )
            {
                msg.Bw.Write( topics.Length );
                for( int i = 0; i < topics.Length; i++ )
                {
                    msg.Bw.Write( topics[i] );
                }
                return msg.SendMessageAsync( _pipe );
            }
        }
    }
}
