using CK.MQTT.Sdk.Bindings;
using CK.MQTT.Sdk.Flows;
using CK.MQTT.Sdk.Packets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk
{
    internal class MqttServerImpl : IMqttServer
    {
        static readonly ITracer _tracer = Tracer.Get<MqttServerImpl>();

        bool _started;
        bool _disposed;
        IDisposable _channelSubscription;
        IDisposable _streamSubscription;

        readonly IEnumerable<IMqttChannelListener> _binaryChannelListeners;
        readonly IPacketChannelFactory _channelFactory;
        readonly IProtocolFlowProvider _flowProvider;
        readonly IConnectionProvider _connectionProvider;
        readonly ISubject<MqttUndeliveredMessage> _undeliveredMessagesListener;
        readonly MqttConfiguration _configuration;
        readonly ISubject<PrivateStream> _privateStreamListener;
        readonly IList<IMqttChannel<IPacket>> _channels = new List<IMqttChannel<IPacket>>();

        internal MqttServerImpl( IMqttChannelListener binaryChannelListener,
            IPacketChannelFactory channelFactory,
            IProtocolFlowProvider flowProvider,
            IConnectionProvider connectionProvider,
            ISubject<MqttUndeliveredMessage> undeliveredMessagesListener,
            MqttConfiguration configuration )
        {
            _privateStreamListener = new Subject<PrivateStream>();
            _binaryChannelListeners = new[] { new PrivateChannelListener( _privateStreamListener, configuration ), binaryChannelListener };
            _channelFactory = channelFactory;
            _flowProvider = flowProvider;
            _connectionProvider = new NotifyingConnectionProvider( this, connectionProvider );
            _undeliveredMessagesListener = undeliveredMessagesListener;
            _configuration = configuration;
        }

        public event EventHandler<MqttUndeliveredMessage> MessageUndelivered = ( sender, args ) => { };

        public event EventHandler<MqttEndpointDisconnected> Stopped = ( sender, args ) => { };

        public event EventHandler<string> ClientConnected;

        public event EventHandler<string> ClientDisconnected;

        public int ActiveConnections { get { return _channels.Where( c => c.IsConnected ).Count(); } }

        public IEnumerable<string> ActiveClients { get { return _connectionProvider.ActiveClients; } }

        public void Start()
        {
            if( _disposed )
                throw new ObjectDisposedException( nameof( MqttServerImpl ) );

            IEnumerable<IObservable<IMqttChannel<byte[]>>> channelStreams = _binaryChannelListeners.Select( listener => listener.GetChannelStream() );

            _channelSubscription = Observable
                .Merge( channelStreams )
                .Subscribe(
                    binaryChannel => ProcessChannel( binaryChannel ),
                    ex => { _tracer.Error( ex ); },
                    () => { }
                );

            _streamSubscription = _undeliveredMessagesListener
                .Subscribe( e =>
                {
                    MessageUndelivered( this, e );
                } );

            _started = true;
        }

        public async Task<IMqttConnectedClient> CreateClientAsync()
        {
            if( _disposed )
                throw new ObjectDisposedException( nameof( MqttServerImpl ) );

            if( !_started )
                throw new InvalidOperationException( ServerProperties.Server_NotStartedError );

            MqttConnectedClientFactory factory = new MqttConnectedClientFactory( _privateStreamListener );
            IMqttConnectedClient client = await factory
                .CreateClientAsync( _configuration );
            string clientId = GetPrivateClientId();

            await client
                .ConnectAsync( new MqttClientCredentials( clientId ) );

            _connectionProvider.RegisterPrivateClient( clientId );

            return client;
        }

        public void Stop() => Dispose();

        public void Dispose()
        {
            if( _disposed ) return;

            try
            {
                _tracer.Info( ClientProperties.Mqtt_Disposing( GetType().FullName ) );

                _streamSubscription?.Dispose();

                foreach( IMqttChannel<IPacket> channel in _channels )
                {
                    channel.Dispose();
                }

                _channels.Clear();

                _channelSubscription?.Dispose();

                foreach( IMqttChannelListener binaryChannelProvider in _binaryChannelListeners )
                {
                    binaryChannelProvider?.Dispose();
                }

                Stopped( this, new MqttEndpointDisconnected( DisconnectedReason.SelfDisconnected ) );
            }
            catch( Exception ex )
            {
                _tracer.Error( ex );
                Stopped( this, new MqttEndpointDisconnected( DisconnectedReason.Error, ex.Message ) );
            }
            finally
            {
                _started = false;
                _disposed = true;
            }
        }

        void ProcessChannel( IMqttChannel<byte[]> binaryChannel )
        {
            _tracer.Verbose( ServerProperties.Server_NewSocketAccepted );

            IMqttChannel<IPacket> packetChannel = _channelFactory.Create( binaryChannel );
            ServerPacketListener packetListener = new ServerPacketListener( packetChannel, _connectionProvider, _flowProvider, _configuration );

            packetListener.Listen();
            packetListener
                .PacketStream
                .Subscribe( _ => { }, ex =>
                {
                    _tracer.Error( ex, ServerProperties.Server_PacketsObservableError );
                    packetChannel.Dispose();
                    packetListener.Dispose();
                }, () =>
                {
                    _tracer.Warn( ServerProperties.Server_PacketsObservableCompleted );
                    packetChannel.Dispose();
                    packetListener.Dispose();
                }
                );

            _channels.Add( packetChannel );
        }

        string GetPrivateClientId()
        {
            string clientId = MqttClient.GetPrivateClientId();

            if( _connectionProvider.PrivateClients.Contains( clientId ) )
            {
                return GetPrivateClientId();
            }

            return clientId;
        }

        void RaiseClientConnected( string clientId )
        {
            ClientConnected?.Invoke( this, clientId );
        }

        void RaiseClientDisconnected( string clientId )
        {
            ClientDisconnected?.Invoke( this, clientId );
        }


        class NotifyingConnectionProvider : IConnectionProvider
        {
            readonly IConnectionProvider _connections;
            readonly MqttServerImpl _server;


            public NotifyingConnectionProvider( MqttServerImpl server, IConnectionProvider connections )
            {
                _server = server;
                _connections = connections;
            }

            public void AddConnection( string clientId, IMqttChannel<IPacket> connection )
            {
                _connections.AddConnection( clientId, connection );
                _server.RaiseClientConnected( clientId );
            }

            public void RemoveConnection( string clientId )
            {
                _connections.RemoveConnection( clientId );
                _server.RaiseClientDisconnected( clientId );
            }

            public IEnumerable<string> ActiveClients => _connections.ActiveClients;

            public int Connections => _connections.Connections;

            public IEnumerable<string> PrivateClients => _connections.PrivateClients;

            public IMqttChannel<IPacket> GetConnection( string clientId ) => _connections.GetConnection( clientId );

            public void RegisterPrivateClient( string clientId ) => _connections.RegisterPrivateClient( clientId );
        }
    }
}
