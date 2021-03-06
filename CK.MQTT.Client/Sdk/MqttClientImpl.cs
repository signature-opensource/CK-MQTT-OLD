using CK.Core;

using CK.MQTT.Sdk.Flows;
using CK.MQTT.Sdk.Packets;
using CK.MQTT.Sdk.Storage;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk
{
    internal class MqttClientImpl : IMqttClient
    {
        bool _disposed;
        bool _isProtocolConnected;
        IPacketListener _packetListener;
        IDisposable _packetsSubscription;
        Subject<Mon<MqttApplicationMessage>> _receiver;
        readonly IActivityMonitor _m;
        readonly IPacketChannelFactory _channelFactory;
        readonly IProtocolFlowProvider _flowProvider;
        readonly IRepository<ClientSession> _sessionRepository;
        readonly IPacketIdProvider _packetIdProvider;
        readonly MqttConfiguration _configuration;
        readonly TaskRunner _clientSender;

        internal MqttClientImpl( IActivityMonitor m,
            IPacketChannelFactory channelFactory,
            IProtocolFlowProvider flowProvider,
            IRepositoryProvider repositoryProvider,
            IPacketIdProvider packetIdProvider,
            MqttConfiguration configuration )
        {
            _receiver = new Subject<Mon<MqttApplicationMessage>>();
            _m = m;
            _channelFactory = channelFactory;
            _flowProvider = flowProvider;
            _sessionRepository = repositoryProvider.GetRepository<ClientSession>();
            _packetIdProvider = packetIdProvider;
            _configuration = configuration;
            _clientSender = TaskRunner.Get();
        }

        public event EventHandler<MqttEndpointDisconnected> Disconnected = ( sender, args ) => { };

        public string Id { get; private set; }

        public bool IsConnected( IActivityMonitor m )
        {
            CheckUnderlyingConnection( m );
            return _isProtocolConnected && Channel.IsConnected;
        }

        public IObservable<Mon<MqttApplicationMessage>> MessageStream => _receiver;

        internal IMqttChannel<IPacket> Channel { get; private set; }

        public async Task<SessionState> ConnectAsync( IActivityMonitor m, MqttClientCredentials credentials, MqttLastWill will = null, bool cleanSession = false )
        {
            if( _disposed ) throw new ObjectDisposedException( GetType().FullName );
            try
            {
                if( IsConnected( m ) )
                {
                    throw new MqttClientException( ClientProperties.Client_AlreadyConnected( Id ) );
                }

                if( string.IsNullOrEmpty( credentials.ClientId ) && !cleanSession )
                {
                    throw new MqttClientException( ClientProperties.Client_AnonymousClientWithoutCleanSession );
                }

                Id = string.IsNullOrEmpty( credentials.ClientId ) ?
                    MqttClient.GetAnonymousClientId() :
                    credentials.ClientId;
                using( m.OpenTrace( $"Connecting to server with id {Id}..." ) )
                {

                    OpenClientSession( m, cleanSession );

                    await InitializeChannelAsync( m );

                    Connect connect = new Connect( Id, cleanSession, MqttProtocol.SupportedLevel )
                    {
                        UserName = credentials.UserName,
                        Password = credentials.Password,
                        Will = will,
                        KeepAlive = _configuration.KeepAliveSecs
                    };

                    await SendPacketAsync( new Mon<IPacket>( m, connect ) );

                    TimeSpan connectTimeout = TimeSpan.FromSeconds( _configuration.WaitTimeoutSecs );
                    ConnectAck ack = (await _packetListener
                        .PacketStream
                        .OfMonitoredType<ConnectAck, IPacket>()
                        .FirstOrDefaultAsync()
                        .Timeout( connectTimeout )).Item;

                    if( ack == null )
                    {
                        throw new MqttClientException( $"The client {Id} has been disconnected while trying to perform the connection" );
                    }

                    if( ack.Status != MqttConnectionStatus.Accepted ) throw new MqttConnectionException( ack.Status );

                    _isProtocolConnected = true;

                    return ack.SessionPresent ? SessionState.SessionPresent : SessionState.CleanSession;
                }
            }
            catch( TimeoutException timeEx )
            {
                Close( m, timeEx );
                throw new MqttClientException( ClientProperties.Client_ConnectionTimeout( Id ), timeEx );
            }
            catch( MqttConnectionException connectionEx )
            {
                Close( m, connectionEx );

                string message = ClientProperties.Client_ConnectNotAccepted( Id, connectionEx.ReturnCode );

                throw new MqttClientException( message, connectionEx );
            }
            catch( MqttClientException clientEx )
            {
                Close( m, clientEx );
                throw;
            }
            catch( Exception ex )
            {
                Close( m, ex );
                throw new MqttClientException( ClientProperties.Client_ConnectionError( Id ), ex );
            }
        }

        public Task<SessionState> ConnectAsync( IActivityMonitor m, MqttLastWill will = null ) =>
            ConnectAsync( m, new MqttClientCredentials(), will, cleanSession: true );

        public async Task SubscribeAsync( IActivityMonitor m, string topicFilter, MqttQualityOfService qos )
        {
            if( _disposed )
            {
                throw new ObjectDisposedException( GetType().FullName );
            }

            try
            {
                ushort packetId = _packetIdProvider.GetPacketId();
                Subscribe subscribe = new Subscribe( packetId, new Subscription( topicFilter, qos ) );

                SubscribeAck ack = default;
                TimeSpan subscribeTimeout = TimeSpan.FromSeconds( _configuration.WaitTimeoutSecs );

                await SendPacketAsync( new Mon<IPacket>( m, subscribe ) );

                ack = (await _packetListener
                    .PacketStream
                    .OfMonitoredType<SubscribeAck, IPacket>()
                    .FirstOrDefaultAsync( x => x.Item.PacketId == packetId )
                    .Timeout( subscribeTimeout )).Item;

                if( ack == null )
                {
                    string message = ClientProperties.Client_SubscriptionDisconnected( Id, topicFilter );

                    m.Error( message );

                    throw new MqttClientException( message );
                }

                if( ack.ReturnCodes.FirstOrDefault() == SubscribeReturnCode.Failure )
                {
                    string message = ClientProperties.Client_SubscriptionRejected( Id, topicFilter );

                    m.Error( message );

                    throw new MqttClientException( message );
                }
            }
            catch( TimeoutException timeEx )
            {
                Close( m, timeEx );

                string message = ClientProperties.Client_SubscribeTimeout( Id, topicFilter );

                throw new MqttClientException( message, timeEx );
            }
            catch( MqttClientException clientEx )
            {
                Close( m, clientEx );
                throw;
            }
            catch( Exception ex )
            {
                Close( m, ex );

                string message = ClientProperties.Client_SubscribeError( Id, topicFilter );

                throw new MqttClientException( message, ex );
            }
        }

        public async Task PublishAsync( IActivityMonitor m, MqttApplicationMessage message, MqttQualityOfService qos, bool retain = false )
        {
            if( _disposed )
            {
                throw new ObjectDisposedException( GetType().FullName );
            }

            try
            {
                ushort? packetId = qos == MqttQualityOfService.AtMostOnce ? null : (ushort?)_packetIdProvider.GetPacketId();
                Publish publish = new Publish( message.Topic, qos, retain, duplicated: false, packetId: packetId )
                {
                    Payload = message.Payload
                };

                PublishSenderFlow senderFlow = _flowProvider.GetFlow<PublishSenderFlow>();

                await _clientSender.Run( async () =>
                 {
                     await senderFlow.SendPublishAsync( m, Id, publish, Channel );
                 } );
            }
            catch( Exception ex )
            {
                Close( m, ex );
                throw;
            }
        }

        public async Task UnsubscribeAsync( IActivityMonitor m, params string[] topics )
        {
            if( _disposed ) throw new ObjectDisposedException( GetType().FullName );

            try
            {
                topics = topics ?? Array.Empty<string>();

                ushort packetId = _packetIdProvider.GetPacketId();
                Unsubscribe unsubscribe = new Unsubscribe( packetId, topics );

                UnsubscribeAck ack = default;
                TimeSpan unsubscribeTimeout = TimeSpan.FromSeconds( _configuration.WaitTimeoutSecs );

                await SendPacketAsync( new Mon<IPacket>( m, unsubscribe ) );

                ack = (await _packetListener
                    .PacketStream
                    .OfMonitoredType<UnsubscribeAck, IPacket>()
                    .FirstOrDefaultAsync( x => x.Item.PacketId == packetId )
                    .Timeout( unsubscribeTimeout )).Item;

                if( ack == null )
                {
                    string message = ClientProperties.Client_UnsubscribeDisconnected( Id, string.Join( ", ", topics ) );

                    m.Error( message );

                    throw new MqttClientException( message );
                }
            }
            catch( TimeoutException timeEx )
            {
                Close( m, timeEx );

                string message = ClientProperties.Client_UnsubscribeTimeout( Id, string.Join( ", ", topics ) );

                m.Error( message );

                throw new MqttClientException( message, timeEx );
            }
            catch( MqttClientException clientEx )
            {
                Close( m, clientEx );
                throw;
            }
            catch( Exception ex )
            {
                Close( m, ex );

                string message = ClientProperties.Client_UnsubscribeError( Id, string.Join( ", ", topics ) );

                m.Error( message );

                throw new MqttClientException( message, ex );
            }
        }

        public async Task DisconnectAsync( IActivityMonitor m )
        {
            try
            {
                if( !IsConnected( m ) )
                {
                    throw new MqttClientException( ClientProperties.Client_AlreadyDisconnected );
                }

                _packetsSubscription?.Dispose();

                await SendPacketAsync( new Mon<IPacket>( m, new Disconnect() ) );

                await _packetListener
                    .PacketStream
                    .LastOrDefaultAsync();

                Close( m, DisconnectedReason.SelfDisconnected );
            }
            catch( Exception ex )
            {
                Close( m, ex );
            }
        }

        void IDisposable.Dispose()
        {
            DisposeAsync( _m, disposing: true ).Wait();
            GC.SuppressFinalize( this );
        }

        protected virtual async Task DisposeAsync( IActivityMonitor m, bool disposing )
        {
            if( _disposed ) return;

            if( disposing )
            {
                if( IsConnected( m ) )
                {
                    await DisconnectAsync( m );
                }

                (_clientSender as IDisposable)?.Dispose();
                _disposed = true;
            }
        }

        void Close( IActivityMonitor m, Exception ex )
        {
            m.Error( ex );
            Close( m, DisconnectedReason.Error, ex.Message );
        }

        void Close( IActivityMonitor m, DisconnectedReason reason, string message = null )
        {
            m.Info( ClientProperties.Client_Closing( Id, reason ) );

            CloseClientSession( m );
            _packetsSubscription?.Dispose();
            _packetListener?.Dispose();
            ResetReceiver();
            Channel?.Dispose();
            _isProtocolConnected = false;
            Id = null;

            Disconnected( this, new MqttEndpointDisconnected( reason, message ) );
        }

        async Task InitializeChannelAsync( IActivityMonitor m )
        {
            Channel = await _channelFactory
                .CreateAsync( m );

            _packetListener = new ClientPacketListener( m, Channel, _flowProvider, _configuration );
            _packetListener.Listen();
            ObservePackets();
        }

        void OpenClientSession( IActivityMonitor m, bool cleanSession )
        {
            ClientSession session = string.IsNullOrEmpty( Id ) ? default : _sessionRepository.Read( Id );

            if( cleanSession && session != null )
            {
                _sessionRepository.Delete( session.Id );
                session = null;

                m.Info( ClientProperties.Client_CleanedOldSession( Id ) );
            }

            if( session == null )
            {
                session = new ClientSession( Id, cleanSession );

                _sessionRepository.Create( session );

                m.Info( ClientProperties.Client_CreatedSession( Id ) );
            }
        }

        void CloseClientSession( IActivityMonitor m )
        {
            ClientSession session = string.IsNullOrEmpty( Id ) ? default : _sessionRepository.Read( Id );

            if( session == null )
            {
                return;
            }

            if( session.Clean )
            {
                _sessionRepository.Delete( session.Id );

                m.Info( ClientProperties.Client_DeletedSessionOnDisconnect( Id ) );
            }
        }

        async Task SendPacketAsync( Mon<IPacket> packet )
        {
            await _clientSender.Run( async () => await Channel.SendAsync( packet ) );
        }

        void CheckUnderlyingConnection( IActivityMonitor m )
        {
            if( _isProtocolConnected && !Channel.IsConnected )
            {
                Close( m, DisconnectedReason.Error, ClientProperties.Client_UnexpectedChannelDisconnection );
            }
        }

        void ObservePackets()
        {
            _packetsSubscription = _packetListener
                .PacketStream
                .Subscribe( packet =>
                 {
                     if( packet.Item.Type == MqttPacketType.Publish )
                     {
                         Publish publish = packet.Item as Publish;
                         MqttApplicationMessage message = new MqttApplicationMessage( publish.Topic, publish.Payload );

                         _receiver.OnNext( new Mon<MqttApplicationMessage>( packet.Monitor, message ) );
                         packet.Monitor.Info( ClientProperties.Client_NewApplicationMessageReceived( Id, publish.Topic ) );
                     }
                 }, ex =>
                 {
                     var m = new ActivityMonitor();//TODO: Remove monitor in onerror.
                     Close( m, ex );
                 }, () =>
                 {
                     var m = new ActivityMonitor();//TODO: Remove monitor in oncomplete.
                     m.Warn( ClientProperties.Client_PacketsObservableCompleted );
                     Close( m, DisconnectedReason.RemoteDisconnected );
                 } );
        }

        void ResetReceiver()
        {
            _receiver?.OnCompleted();
            _receiver = new Subject<Mon<MqttApplicationMessage>>();
        }
    }
}
