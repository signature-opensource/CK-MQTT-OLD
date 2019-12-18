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
                using( MemoryStream msg = await _pipe.ReadMessageAsync( token ) )
                using( CKBinaryReader br = new CKBinaryReader( msg ) )
                {
                    switch( br.ReadEnum<RelayHeader>() )
                    {
                        case RelayHeader.Disconnected:
                            Disconnected?.Invoke( this, br.ReadDisconnectEvent() );
                            break;
                        case RelayHeader.MessageEvent:
                            _receiver.OnNext( br.ReadApplicationMessage() );
                            break;
                        default:
                            throw new InvalidDataException( "Unknown Relay Header." );
                    }
                    if( msg.Length != msg.Position ) throw new DataMisalignedException( "Expected to read the entierty of the stream." );
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
            await _pipe.ConnectAsync( _waitTimeoutSecs * 1000 );
            _pipe.ReadMode = PipeTransmissionMode.Message;
            if( credentials == null )
            {
                using( MessageFormatter mf = new MessageFormatter() )//anonymous
                {
                    mf.Bw.WriteEnum( StubClientHeader.ConnectAnonymous );
                    mf.Bw.Write( will );
                    await mf.SendMessageAsync( _pipe ); //I would like async disposable :'(
                }
            }
            else
            {
                using( MessageFormatter mf = new MessageFormatter() )
                {
                    mf.Bw.WriteEnum( StubClientHeader.Connect );
                    mf.Bw.Write( credentials );
                    mf.Bw.Write( will );
                    mf.Bw.Write( cleanSession );
                    await mf.SendMessageAsync( _pipe );
                }
            }
            using( var msgStream = await _pipe.ReadMessageAsync( CancellationToken.None ) )
            using( var br = new CKBinaryReader( msgStream ) )
            {
                SessionState state = br.ReadEnum<SessionState>();
                if( msgStream.Position != msgStream.Length ) throw new DataMisalignedException( "More data to read than expected." );
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
            //TODO: what should i do there ?
            //return _pipeFormatter.SendPayloadAsync( StubClientHeader.Disconnect );
        }

        public void Dispose()
        {
            _listenerCancel?.Cancel();
            _listener?.Wait();
            _pipe.Dispose();
        }

        public Task PublishAsync( MqttApplicationMessage message, MqttQualityOfService qos, bool retain = false )
        {
            using(var msg = new MessageFormatter())
            {
                msg.Bw.WriteEnum( StubClientHeader.Publish );
                msg.Bw.Write( message );
                msg.Bw.WriteEnum( qos );
                msg.Bw.Write( retain );
                return msg.SendMessageAsync( _pipe );
            }
        }

        public Task SubscribeAsync( string topicFilter, MqttQualityOfService qos )
        {
            using(var msg = new MessageFormatter())
            {
                msg.Bw.WriteEnum( StubClientHeader.Subscribe );
                msg.Bw.Write( topicFilter);
                msg.Bw.WriteEnum(qos);
                return msg.SendMessageAsync( _pipe );
            }
        }

        public Task UnsubscribeAsync( params string[] topics )
        {
            using(var msg = new MessageFormatter())
            {
                msg.Bw.WriteEnum( StubClientHeader.Unsubscribe );
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
