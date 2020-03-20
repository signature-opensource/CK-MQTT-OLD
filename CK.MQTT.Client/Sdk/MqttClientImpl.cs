using CK.Core;
using CK.MQTT.Sdk.Flows;
using CK.MQTT.Sdk.Packets;
using CK.MQTT.Sdk.Storage;
using System;
using System.Collections.Generic;
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
            _m = m;
            _channelFactory = channelFactory;
            _flowProvider = flowProvider;
            _sessionRepository = repositoryProvider.GetRepository<ClientSession>();
            _packetIdProvider = packetIdProvider;
            _configuration = configuration;
            _clientSender = TaskRunner.Get();
        }

        SequentialEventHandlerSender<MqttEndpointDisconnected> _eSeqDisconnect;
        public event SequentialEventHandler<MqttEndpointDisconnected> Disconnected
        {
            add => _eSeqDisconnect.Add( value );
            remove => _eSeqDisconnect.Remove( value );
        }

        SequentialEventHandlerAsyncSender<MqttEndpointDisconnected> _eSeqDisconnectAsync;
        public event SequentialEventHandlerAsync<MqttEndpointDisconnected> DisconnectedAsync
        {
            add => _eSeqDisconnectAsync.Add( value );
            remove => _eSeqDisconnectAsync.Remove( value );
        }

        ParallelEventHandlerAsyncSender<MqttEndpointDisconnected> _eParDisconnectAsync;
        public event ParallelEventHandlerAsync<MqttEndpointDisconnected> ParallelDisconnectedAsync
        {
            add => _eParDisconnectAsync.Add( value );
            remove => _eParDisconnectAsync.Add( value );
        }

        public Task RaiseDisconnectAsync( IActivityMonitor m, MqttEndpointDisconnected disconnect )
        {
            Task task = _eParDisconnectAsync.RaiseAsync( m, this, disconnect );
            _eSeqDisconnect.Raise( m, this, disconnect );
            return Task.WhenAll( task, _eSeqDisconnectAsync.RaiseAsync( m, this, disconnect ) );
        }

        ParallelEventHandlerAsyncSender<MqttApplicationMessage> _eParMessageAsync;
        public event ParallelEventHandlerAsync<MqttApplicationMessage> ParallelMessageReceivedAsync
        {
            add => _eParMessageAsync.Add( value );
            remove => _eParMessageAsync.Remove( value );
        }

        SequentialEventHandlerSender<MqttApplicationMessage> _eSeqMessage;
        public event SequentialEventHandler<MqttApplicationMessage> MessageReceived
        {
            add => _eSeqMessage.Add( value );
            remove => _eSeqMessage.Remove( value );
        }

        SequentialEventHandlerAsyncSender<MqttApplicationMessage> _eSeqMessageAsync;
        public event SequentialEventHandlerAsync<MqttApplicationMessage> MessageReceivedAsync
        {
            add => _eSeqMessageAsync.Add( value );
            remove => _eSeqMessageAsync.Remove( value );
        }

        public Task RaiseMessageAsync( IActivityMonitor m, MqttApplicationMessage message )
        {
            Task task = _eParMessageAsync.RaiseAsync( m, this, message );
            _eSeqMessage.Raise( m, this, message );
            return Task.WhenAll( task, _eSeqMessageAsync.RaiseAsync( m, this, message ) );
        }

        public string ClientId { get; private set; }

        public bool CheckConnection( IActivityMonitor m )
        {
            CheckUnderlyingConnection( m );
            return _isProtocolConnected && Channel.IsConnected;
        }

        internal IMqttChannel<IPacket> Channel { get; private set; }

        public async Task<SessionState> ConnectAsync( IActivityMonitor m, MqttClientCredentials credentials, MqttLastWill will = null, bool cleanSession = false )
        {
            if( _disposed ) throw new ObjectDisposedException( GetType().FullName );
            try
            {
                if( CheckConnection( m ) )
                {
                    throw new MqttClientException( ClientProperties.Client_AlreadyConnected( ClientId ) );
                }

                if( string.IsNullOrEmpty( credentials.ClientId ) && !cleanSession )
                {
                    throw new MqttClientException( ClientProperties.Client_AnonymousClientWithoutCleanSession );
                }

                ClientId = string.IsNullOrEmpty( credentials.ClientId ) ?
                    MqttClient.GetAnonymousClientId() :
                    credentials.ClientId;
                using( m.OpenTrace( $"Connecting to server with id {ClientId}..." ) )
                {

                    OpenClientSession( m, cleanSession );

                    await InitializeChannelAsync( m );

                    Connect connect = new Connect( ClientId, cleanSession, MqttProtocol.SupportedLevel )
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
                        .ObserveOn( NewThreadScheduler.Default )
                        .OfMonitoredType<ConnectAck, IPacket>()
                        .FirstOrDefaultAsync()
                        .Timeout( connectTimeout )).Item;

                    if( ack == null )
                    {
                        throw new MqttClientException( $"The client {ClientId} has been disconnected while trying to perform the connection" );
                    }

                    if( ack.Status != MqttConnectionStatus.Accepted ) throw new MqttConnectionException( ack.Status );

                    _isProtocolConnected = true;

                    return ack.SessionPresent ? SessionState.SessionPresent : SessionState.CleanSession;
                }
            }
            catch( TimeoutException timeEx )
            {
                Close( m, timeEx );
                throw new MqttClientException( ClientProperties.Client_ConnectionTimeout( ClientId ), timeEx );
            }
            catch( MqttConnectionException connectionEx )
            {
                Close( m, connectionEx );

                string message = ClientProperties.Client_ConnectNotAccepted( ClientId, connectionEx.ReturnCode );

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
                throw new MqttClientException( ClientProperties.Client_ConnectionError( ClientId ), ex );
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
                    .ObserveOn( NewThreadScheduler.Default )
                    .OfMonitoredType<SubscribeAck, IPacket>()
                    .FirstOrDefaultAsync( x => x.Item.PacketId == packetId )
                    .Timeout( subscribeTimeout )).Item;

                if( ack == null )
                {
                    string message = ClientProperties.Client_SubscriptionDisconnected( ClientId, topicFilter );

                    m.Error( message );

                    throw new MqttClientException( message );
                }

                if( ack.ReturnCodes.FirstOrDefault() == SubscribeReturnCode.Failure )
                {
                    string message = ClientProperties.Client_SubscriptionRejected( ClientId, topicFilter );

                    m.Error( message );

                    throw new MqttClientException( message );
                }
            }
            catch( TimeoutException timeEx )
            {
                Close( m, timeEx );

                string message = ClientProperties.Client_SubscribeTimeout( ClientId, topicFilter );

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

                string message = ClientProperties.Client_SubscribeError( ClientId, topicFilter );

                throw new MqttClientException( message, ex );
            }
        }

        public async Task PublishAsync( IActivityMonitor m, string topic, ReadOnlyMemory<byte> payload, MqttQualityOfService qos, bool retain = false )
        {
            if( _disposed )
            {
                throw new ObjectDisposedException( GetType().FullName );
            }

            try
            {
                ushort? packetId = qos == MqttQualityOfService.AtMostOnce ? null : (ushort?)_packetIdProvider.GetPacketId();
                Publish publish = new Publish( topic, payload, qos, retain, duplicated: false, packetId: packetId );

                PublishSenderFlow senderFlow = _flowProvider.GetFlow<PublishSenderFlow>();

                await _clientSender.Run( async () =>
                 {
                     await senderFlow.SendPublishAsync( m, ClientId, publish, Channel );
                 } );
            }
            catch( Exception ex )
            {
                Close( m, ex );
                throw;
            }
        }

        public async Task UnsubscribeAsync( IActivityMonitor m, IEnumerable<string> topics )
        {
            if( _disposed ) throw new ObjectDisposedException( GetType().FullName );

            try
            {
                topics ??= Array.Empty<string>();

                ushort packetId = _packetIdProvider.GetPacketId();
                Unsubscribe unsubscribe = new Unsubscribe( packetId, topics );

                UnsubscribeAck ack = default;
                TimeSpan unsubscribeTimeout = TimeSpan.FromSeconds( _configuration.WaitTimeoutSecs );

                await SendPacketAsync( new Mon<IPacket>( m, unsubscribe ) );

                ack = (await _packetListener
                    .PacketStream
                    .ObserveOn( NewThreadScheduler.Default )
                    .OfMonitoredType<UnsubscribeAck, IPacket>()
                    .FirstOrDefaultAsync( x => x.Item.PacketId == packetId )
                    .Timeout( unsubscribeTimeout )).Item;

                if( ack == null )
                {
                    string message = ClientProperties.Client_UnsubscribeDisconnected( ClientId, string.Join( ", ", topics ) );

                    m.Error( message );

                    throw new MqttClientException( message );
                }
            }
            catch( TimeoutException timeEx )
            {
                Close( m, timeEx );

                string message = ClientProperties.Client_UnsubscribeTimeout( ClientId, string.Join( ", ", topics ) );

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

                string message = ClientProperties.Client_UnsubscribeError( ClientId, string.Join( ", ", topics ) );

                m.Error( message );

                throw new MqttClientException( message, ex );
            }
        }

        public async Task DisconnectAsync( IActivityMonitor m )
        {
            try
            {
                if( !CheckConnection( m ) )
                {
                    throw new MqttClientException( ClientProperties.Client_AlreadyDisconnected );
                }

                _packetsSubscription?.Dispose();

                await SendPacketAsync( new Mon<IPacket>( m, new Disconnect() ) );

                await _packetListener
                    .PacketStream
                    .LastOrDefaultAsync();

                await CloseAsync( m, DisconnectedReason.SelfDisconnected );
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
                if( CheckConnection( m ) )
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
            CloseAsync( m, DisconnectedReason.Error, ex.Message );
        }

        Task CloseAsync( IActivityMonitor m, DisconnectedReason reason, string message = null )
        {
            m.Info( ClientProperties.Client_Closing( ClientId, reason ) );

            CloseClientSession( m );
            _packetsSubscription?.Dispose();
            _packetListener?.Dispose();
            Channel?.Dispose();
            _isProtocolConnected = false;
            ClientId = null;
            var disconnect = new MqttEndpointDisconnected( reason, message );
            return RaiseDisconnectAsync( m, disconnect );
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
            ClientSession session = string.IsNullOrEmpty( ClientId ) ? default : _sessionRepository.Read( ClientId );

            if( cleanSession && session != null )
            {
                _sessionRepository.Delete( session.Id );
                session = null;

                m.Info( ClientProperties.Client_CleanedOldSession( ClientId ) );
            }

            if( session == null )
            {
                session = new ClientSession( ClientId, cleanSession );

                _sessionRepository.Create( session );

                m.Info( ClientProperties.Client_CreatedSession( ClientId ) );
            }
        }

        void CloseClientSession( IActivityMonitor m )
        {
            ClientSession session = string.IsNullOrEmpty( ClientId ) ? default : _sessionRepository.Read( ClientId );

            if( session == null )
            {
                return;
            }

            if( session.Clean )
            {
                _sessionRepository.Delete( session.Id );

                m.Info( ClientProperties.Client_DeletedSessionOnDisconnect( ClientId ) );
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
                CloseAsync( m, DisconnectedReason.Error, ClientProperties.Client_UnexpectedChannelDisconnection );
            }
        }

        void ObservePackets()
        {
            _packetsSubscription = _packetListener
                .PacketStream
                .ObserveOn( NewThreadScheduler.Default )
                .Subscribe( packet =>
                 {
                     if( packet.Item.Type == MqttPacketType.Publish )
                     {
                         Publish publish = packet.Item as Publish;
                         MqttApplicationMessage message = new MqttApplicationMessage( publish.Topic, publish.Payload );
                         using( packet.Monitor.OpenInfo( ClientProperties.Client_NewApplicationMessageReceived( ClientId, publish.Topic ) ) )
                         {
                             RaiseMessageAsync( packet.Monitor, message ).Wait();
                         }
                     }
                 }, ex =>
                 {
                     var m = new ActivityMonitor();//TODO: Remove monitor in onerror.
                     Close( m, ex );
                 }, () =>
                 {
                     var m = new ActivityMonitor();//TODO: Remove monitor in oncomplete.
                     m.Warn( ClientProperties.Client_PacketsObservableCompleted );
                     CloseAsync( m, DisconnectedReason.RemoteDisconnected );
                 } );
        }
    }
}
