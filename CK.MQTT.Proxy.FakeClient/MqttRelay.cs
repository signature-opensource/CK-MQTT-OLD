//using CK.Core;
//using Microsoft.Extensions.Hosting;
//using System;
//using System.Buffers;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;

//namespace CK.MQTT.Proxy.FakeClient
//{
//    public class MqttRelay : IHostedService, IDisposable
//    {
//        readonly IActivityMonitor _m;
//        readonly MqttClientCredentials _credentials;
//        readonly MqttLastWill _lastWill;
//        readonly bool _cleanSession;
//        readonly string _pipeName;
//        readonly IMqttClient _client;
//        Task _acceptClientsTask;
//        List<RelayConnection> _connections;
//        readonly IDisposable _observing;
//        CancellationTokenSource _tokenSource;
//        readonly bool _anonymous;
//        public MqttRelay(
//            IActivityMonitor m,
//            IMqttClient client,
//            MqttClientCredentials credentials,
//            bool cleanSession,
//            string pipeName = "ck_mqtt",
//            MqttLastWill lastWill = null ) : this( m, client, pipeName, lastWill )
//        {
//            _credentials = credentials;
//            _cleanSession = cleanSession;
//            _anonymous = false;
//        }

//        internal void Disconnect( RelayConnection connection )
//        {
//            lock( _connections )
//            {
//                _connections.Remove( connection );
//            }
//        }

//        public MqttRelay( IActivityMonitor m, IMqttClient client, string pipeName = "ck_mqtt", MqttLastWill lastWill = null )
//        {
//            _m = m;
//            _lastWill = lastWill;
//            _client = client;
//            _observing = _client.MessageStream.Subscribe( MessageReceived );
//            client.Disconnected += Client_Disconnected;
//            _anonymous = true;
//            _pipeName = pipeName;
//        }

//        void Client_Disconnected( object sender, MqttEndpointDisconnected e )
//        {
//            using( var msg = new MessageFormatter( RelayHeader.Disconnected ) )
//            {
//                msg.Bw.Write( e );
//                using( IMemoryOwner<byte> buffer = msg.FormatMessage() )
//                {
//                    IEnumerable<Task> tasks;
//                    lock( _connections )
//                    {
//                        tasks = _connections.Select( s => s.SendMessage( buffer.Memory, default ).AsTask() ).ToList();
//                    }
//                    Task.WhenAll( tasks );
//                }
//            }
//        }

//        void MessageReceived( MqttApplicationMessage msg )
//        {
//            using( var output = new MessageFormatter( RelayHeader.MessageEvent ) )
//            {
//                output.Bw.Write( msg );
//                using( var buffer = output.FormatMessage() )
//                {
//                    IEnumerable<Task> tasks;
//                    lock( _connections )
//                    {
//                        tasks = _connections.Select( s => s.SendMessage( buffer.Memory, default ).AsTask() );
//                    }
//                    Task.WhenAll( tasks );
//                }
//            }
//        }

//        async Task AcceptClients( CancellationToken cancellationToken )
//        {
//            while( !cancellationToken.IsCancellationRequested )
//            {
//                RelayConnection connection = await RelayConnection.CreateRelayConnection(this, _client, _pipeName, cancellationToken );
//                if( cancellationToken.IsCancellationRequested ) return;
//                lock( _connections )
//                {
//                    _connections.Add( connection );

//                }
//            }
//        }

//        public async Task StartAsync( CancellationToken cancellationToken )
//        {
//            _connections = new List<RelayConnection>();
//            _tokenSource = new CancellationTokenSource();
//            if( _anonymous )
//            {
//                await _client.ConnectAsync( _lastWill );
//            }
//            else
//            {
//                await _client.ConnectAsync( _credentials, _lastWill, _cleanSession );
//            }
//            _acceptClientsTask = AcceptClients( _tokenSource.Token );
//        }

//        public async Task StopAsync( CancellationToken cancellationToken )
//        {
//            _tokenSource.Cancel();
//            try
//            {
//                await Task.WhenAny( Task.FromCanceled( cancellationToken ), _acceptClientsTask );
//            }
//            catch( TaskCanceledException e )
//            {
//                _m.Warn( e );
//            }
//            List<Task<bool>> safeTasks;
//            lock( _connections )
//            {
//                safeTasks = _connections.Select( s => s.StopAsync().ContinueWith( t => t.IsCompleted ) ).ToList(); //Should not throw an exception now.
//            }
//            var tasksResultsTask = Task.WhenAll( safeTasks );
//            try
//            {
//                await Task.WhenAny( tasksResultsTask, Task.FromCanceled( cancellationToken ) );
//            }
//            catch( TaskCanceledException e )
//            {
//                _m.Warn( e );
//            }
//            if( cancellationToken.IsCancellationRequested ) return;
//            if( tasksResultsTask.Result.Contains( false ) )
//            {
//                //[Generic "this scenario should never happen" comment.]
//                throw new InvalidOperationException( "A task didn't completed, but was cancelled and awaited." );
//            }
//        }

//        public void Dispose()
//        {
//            StopAsync( CancellationToken.None ).Wait( 100 );
//            _client.Disconnected -= Client_Disconnected;
//            _observing.Dispose();
//        }
//    }
//}
