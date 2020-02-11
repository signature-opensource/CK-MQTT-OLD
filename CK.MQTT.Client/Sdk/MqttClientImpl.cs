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
        static readonly ITracer _tracer = Tracer.Get<MqttClientImpl>();

        bool _disposed;
        bool _isProtocolConnected;
        IPacketListener _packetListener;
        IDisposable _packetsSubscription;
        Subject<MqttApplicationMessage> _receiver;

        readonly IPacketChannelFactory _channelFactory;
        readonly IProtocolFlowProvider _flowProvider;
        readonly IRepository<ClientSession> _sessionRepository;
        readonly IPacketIdProvider _packetIdProvider;
        readonly MqttConfiguration _configuration;
        readonly TaskRunner _clientSender;

        internal MqttClientImpl( IPacketChannelFactory channelFactory,
            IProtocolFlowProvider flowProvider,
            IRepositoryProvider repositoryProvider,
            IPacketIdProvider packetIdProvider,
            MqttConfiguration configuration )
        {
            _receiver = new Subject<MqttApplicationMessage>();
            _channelFactory = channelFactory;
            _flowProvider = flowProvider;
            _sessionRepository = repositoryProvider.GetRepository<ClientSession>();
            _packetIdProvider = packetIdProvider;
            _configuration = configuration;
            _clientSender = TaskRunner.Get();
        }

        public event EventHandler<MqttEndpointDisconnected> Disconnected = ( sender, args ) => { };

        public string Id { get; private set; }

        public bool IsConnected
        {
            get
            {
                CheckUnderlyingConnection();
                return _isProtocolConnected && Channel.IsConnected;
            }
            private set => _isProtocolConnected = value;
        }

        public IObservable<MqttApplicationMessage> MessageStream => _receiver;

        internal IMqttChannel<IPacket> Channel { get; private set; }

        public async Task<SessionState> ConnectAsync( MqttClientCredentials credentials, MqttLastWill will = null, bool cleanSession = false )
        {
            if( _disposed ) throw new ObjectDisposedException( GetType().FullName );

            try
            {
                if( IsConnected )
                {
                    throw new MqttClientException( string.Format( Properties.Resources.GetString( "Client_AlreadyConnected" ), Id ) );
                }

                if( string.IsNullOrEmpty( credentials.ClientId ) && !cleanSession )
                {
                    throw new MqttClientException( Properties.Resources.GetString( "Client_AnonymousClientWithoutCleanSession" ) );
                }

                Id = string.IsNullOrEmpty( credentials.ClientId ) ?
                    MqttClient.GetAnonymousClientId() :
                    credentials.ClientId;

                OpenClientSession( cleanSession );

                await InitializeChannelAsync();

                Connect connect = new Connect( Id, cleanSession )
                {
                    UserName = credentials.UserName,
                    Password = credentials.Password,
                    Will = will,
                    KeepAlive = _configuration.KeepAliveSecs
                };

                await SendPacketAsync( connect );

                TimeSpan connectTimeout = TimeSpan.FromSeconds( _configuration.WaitTimeoutSecs );
                ConnectAck ack = await _packetListener
                    .PacketStream
                    .ObserveOn( NewThreadScheduler.Default )
                    .OfType<ConnectAck>()
                    .FirstOrDefaultAsync()
                    .Timeout( connectTimeout );

                if( ack == null )
                {
                    string message = string.Format( Properties.Resources.GetString( "Client_ConnectionDisconnected" ), Id );

                    throw new MqttClientException( message );
                }

                if( ack.Status != MqttConnectionStatus.Accepted ) throw new MqttConnectionException( ack.Status );

                IsConnected = true;

                return ack.SessionPresent ? SessionState.SessionPresent : SessionState.CleanSession;
            }
            catch( TimeoutException timeEx )
            {
                Close( timeEx );
                throw new MqttClientException( string.Format( Properties.Resources.GetString( "Client_ConnectionTimeout" ), Id ), timeEx );
            }
            catch( MqttConnectionException connectionEx )
            {
                Close( connectionEx );

                string message = string.Format( Properties.Resources.GetString( "Client_ConnectNotAccepted" ), Id, connectionEx.ReturnCode );

                throw new MqttClientException( message, connectionEx );
            }
            catch( MqttClientException clientEx )
            {
                Close( clientEx );
                throw;
            }
            catch( Exception ex )
            {
                Close( ex );
                throw new MqttClientException( string.Format( Properties.Resources.GetString( "Client_ConnectionError" ), Id ), ex );
            }
        }

        public Task<SessionState> ConnectAsync( MqttLastWill will = null ) =>
            ConnectAsync( new MqttClientCredentials(), will, cleanSession: true );

        public async Task SubscribeAsync( string topicFilter, MqttQualityOfService qos )
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

                await SendPacketAsync( subscribe );

                ack = await _packetListener
                    .PacketStream
                    .ObserveOn( NewThreadScheduler.Default )
                    .OfType<SubscribeAck>()
                    .FirstOrDefaultAsync( x => x.PacketId == packetId )
                    .Timeout( subscribeTimeout );

                if( ack == null )
                {
                    string message = string.Format( Properties.Resources.GetString( "Client_SubscriptionDisconnected" ), Id, topicFilter );

                    _tracer.Error( message );

                    throw new MqttClientException( message );
                }

                if( ack.ReturnCodes.FirstOrDefault() == SubscribeReturnCode.Failure )
                {
                    string message = string.Format( Properties.Resources.GetString( "Client_SubscriptionRejected" ), Id, topicFilter );

                    _tracer.Error( message );

                    throw new MqttClientException( message );
                }
            }
            catch( TimeoutException timeEx )
            {
                Close( timeEx );

                string message = string.Format( Properties.Resources.GetString( "Client_SubscribeTimeout" ), Id, topicFilter );

                throw new MqttClientException( message, timeEx );
            }
            catch( MqttClientException clientEx )
            {
                Close( clientEx );
                throw;
            }
            catch( Exception ex )
            {
                Close( ex );

                string message = string.Format( Properties.Resources.GetString( "Client_SubscribeError" ), Id, topicFilter );

                throw new MqttClientException( message, ex );
            }
        }

        public async Task PublishAsync( MqttApplicationMessage message, MqttQualityOfService qos, bool retain = false )
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
                     await senderFlow.SendPublishAsync( Id, publish, Channel );
                 } );
            }
            catch( Exception ex )
            {
                Close( ex );
                throw;
            }
        }

        public async Task UnsubscribeAsync( params string[] topics )
        {
            if( _disposed ) throw new ObjectDisposedException( GetType().FullName );

            try
            {
                topics = topics ?? Array.Empty<string>();

                ushort packetId = _packetIdProvider.GetPacketId();
                Unsubscribe unsubscribe = new Unsubscribe( packetId, topics );

                UnsubscribeAck ack = default;
                TimeSpan unsubscribeTimeout = TimeSpan.FromSeconds( _configuration.WaitTimeoutSecs );

                await SendPacketAsync( unsubscribe );

                ack = await _packetListener
                    .PacketStream
                    .ObserveOn( NewThreadScheduler.Default )
                    .OfType<UnsubscribeAck>()
                    .FirstOrDefaultAsync( x => x.PacketId == packetId )
                    .Timeout( unsubscribeTimeout );

                if( ack == null )
                {
                    string message = string.Format( Properties.Resources.GetString( "Client_UnsubscribeDisconnected" ), Id, string.Join( ", ", topics ) );

                    _tracer.Error( message );

                    throw new MqttClientException( message );
                }
            }
            catch( TimeoutException timeEx )
            {
                Close( timeEx );

                string message = string.Format( Properties.Resources.GetString( "Client_UnsubscribeTimeout" ), Id, string.Join( ", ", topics ) );

                _tracer.Error( message );

                throw new MqttClientException( message, timeEx );
            }
            catch( MqttClientException clientEx )
            {
                Close( clientEx );
                throw;
            }
            catch( Exception ex )
            {
                Close( ex );

                string message = string.Format( Properties.Resources.GetString( "Client_UnsubscribeError" ), Id, string.Join( ", ", topics ) );

                _tracer.Error( message );

                throw new MqttClientException( message, ex );
            }
        }

        public async Task DisconnectAsync()
        {
            try
            {
                if( !IsConnected )
                {
                    throw new MqttClientException( Properties.Resources.GetString( "Client_AlreadyDisconnected" ) );
                }

                _packetsSubscription?.Dispose();

                await SendPacketAsync( new Disconnect() );

                await _packetListener
                    .PacketStream
                    .LastOrDefaultAsync();

                Close( DisconnectedReason.SelfDisconnected );
            }
            catch( Exception ex )
            {
                Close( ex );
            }
        }

        void IDisposable.Dispose()
        {
            DisposeAsync( disposing: true ).Wait();
            GC.SuppressFinalize( this );
        }

        protected virtual async Task DisposeAsync( bool disposing )
        {
            if( _disposed ) return;

            if( disposing )
            {
                if( IsConnected )
                {
                    await DisconnectAsync();
                }

                (_clientSender as IDisposable)?.Dispose();
                _disposed = true;
            }
        }

        void Close( Exception ex )
        {
            _tracer.Error( ex );
            Close( DisconnectedReason.Error, ex.Message );
        }

        void Close( DisconnectedReason reason, string message = null )
        {
            _tracer.Info( Properties.Resources.GetString( "Client_Closing" ), Id, reason );

            CloseClientSession();
            _packetsSubscription?.Dispose();
            _packetListener?.Dispose();
            ResetReceiver();
            Channel?.Dispose();
            IsConnected = false;
            Id = null;

            Disconnected( this, new MqttEndpointDisconnected( reason, message ) );
        }

        async Task InitializeChannelAsync()
        {
            Channel = await _channelFactory
                .CreateAsync();

            _packetListener = new ClientPacketListener( Channel, _flowProvider, _configuration );
            _packetListener.Listen();
            ObservePackets();
        }

        void OpenClientSession( bool cleanSession )
        {
            ClientSession session = string.IsNullOrEmpty( Id ) ? default : _sessionRepository.Read( Id );

            if( cleanSession && session != null )
            {
                _sessionRepository.Delete( session.Id );
                session = null;

                _tracer.Info( Properties.Resources.GetString( "Client_CleanedOldSession" ), Id );
            }

            if( session == null )
            {
                session = new ClientSession( Id, cleanSession );

                _sessionRepository.Create( session );

                _tracer.Info( Properties.Resources.GetString( "Client_CreatedSession" ), Id );
            }
        }

        void CloseClientSession()
        {
            ClientSession session = string.IsNullOrEmpty( Id ) ? default : _sessionRepository.Read( Id );

            if( session == null )
            {
                return;
            }

            if( session.Clean )
            {
                _sessionRepository.Delete( session.Id );

                _tracer.Info( Properties.Resources.GetString( "Client_DeletedSessionOnDisconnect" ), Id );
            }
        }

        async Task SendPacketAsync( IPacket packet )
        {
            await _clientSender.Run( async () => await Channel.SendAsync( packet ) );
        }

        void CheckUnderlyingConnection()
        {
            if( _isProtocolConnected && !Channel.IsConnected )
            {
                Close( DisconnectedReason.Error, Properties.Resources.GetString( "Client_UnexpectedChannelDisconnection" ) );
            }
        }

        void ObservePackets()
        {
            _packetsSubscription = _packetListener
                .PacketStream
                .ObserveOn( NewThreadScheduler.Default )
                .Subscribe( packet =>
                 {
                     if( packet.Type == MqttPacketType.Publish )
                     {
                         Publish publish = packet as Publish;
                         MqttApplicationMessage message = new MqttApplicationMessage( publish.Topic, publish.Payload );

                         _receiver.OnNext( message );
                         _tracer.Info( Properties.Resources.GetString( "Client_NewApplicationMessageReceived" ), Id, publish.Topic );
                     }
                 }, ex =>
                 {
                     Close( ex );
                 }, () =>
                 {
                     _tracer.Warn( Properties.Resources.GetString( "Client_PacketsObservableCompleted" ) );
                     Close( DisconnectedReason.RemoteDisconnected );
                 } );
        }

        void ResetReceiver()
        {
            _receiver?.OnCompleted();
            _receiver = new Subject<MqttApplicationMessage>();
        }
    }
}
