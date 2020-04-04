using CK.Core;
using CK.MQTT.Packets;
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
    internal class MqttClientImplOld : IMqttClient
    {
        readonly IPacketChannelFactory _channelFactory;
        readonly IProtocolFlowProvider _flowProvider;
        readonly IRepository<ClientSession> _sessionRepository;
        readonly MqttConfiguration _configuration;
        readonly IPacketIdProvider _packetIdProvider;

        bool _isProtocolConnected;
        IDisposable _packetsSubscription;

        internal MqttClientImplOld(
            IPacketChannelFactory channelFactory,
            IProtocolFlowProvider flowProvider,
            IRepositoryProvider repositoryProvider,
            IPacketIdProvider packetIdProvider,
            MqttConfiguration configuration )
        {
            _channelFactory = channelFactory;
            _flowProvider = flowProvider;
            _sessionRepository = repositoryProvider.GetRepository<ClientSession>();
            _packetIdProvider = packetIdProvider;
            _configuration = configuration;
        }

        internal IMqttChannel<IPacket> Channel { get; private set; }
        public string ClientId { get; private set; }


        #region Events


        SequentialEventHandlerSender<IMqttClient,MqttEndpointDisconnected> _eSeqDisconnect = new SequentialEventHandlerSender<IMqttClient, MqttEndpointDisconnected>();
        public event SequentialEventHandler<IMqttClient, MqttEndpointDisconnected> Disconnected
        {
            add => _eSeqDisconnect.Add( value );
            remove => _eSeqDisconnect.Remove( value );
        }

        SequentialEventHandlerAsyncSender<IMqttClient,MqttEndpointDisconnected> _eSeqDisconnectAsync;
        public event SequentialEventHandlerAsync<IMqttClient, MqttEndpointDisconnected> DisconnectedAsync
        {
            add => _eSeqDisconnectAsync.Add( value );
            remove => _eSeqDisconnectAsync.Remove( value );
        }

        ParallelEventHandlerAsyncSender<IMqttClient, MqttEndpointDisconnected> _eParDisconnectAsync;
        public event ParallelEventHandlerAsync<IMqttClient, MqttEndpointDisconnected> ParallelDisconnectedAsync
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

        ParallelEventHandlerAsyncSender<IMqttClient, MqttApplicationMessage> _eParMessageAsync;
        public event ParallelEventHandlerAsync<IMqttClient, MqttApplicationMessage> ParallelMessageReceivedAsync
        {
            add => _eParMessageAsync.Add( value );
            remove => _eParMessageAsync.Remove( value );
        }

        SequentialEventHandlerSender<IMqttClient, MqttApplicationMessage> _eSeqMessage = new SequentialEventHandlerSender<IMqttClient, MqttApplicationMessage>();
        public event SequentialEventHandler<IMqttClient, MqttApplicationMessage> MessageReceived
        {
            add => _eSeqMessage.Add( value );
            remove => _eSeqMessage.Remove( value );
        }

        public Task<MqttApplicationMessage?> WaitMessageReceivedAsync( Func<MqttApplicationMessage, bool>? predicate = null, int timeoutMillisecond = -1 )
        {
            return _eSeqMessage.WaitAsync( predicate, timeoutMillisecond );
        }

        SequentialEventHandlerAsyncSender<IMqttClient, MqttApplicationMessage> _eSeqMessageAsync;
        public event SequentialEventHandlerAsync<IMqttClient, MqttApplicationMessage> MessageReceivedAsync
        {
            add => _eSeqMessageAsync.Add( value );
            remove => _eSeqMessageAsync.Remove( value );
        }

        /// <summary>
        /// Raise message to events handlers.
        /// </summary>
        /// <param name="m">The monitor.</param>
        /// <param name="message">The message to raise.</param>
        /// <returns></returns>
        Task RaiseMessageAsync( IActivityMonitor m, MqttApplicationMessage message )
        {
            Task task = _eParMessageAsync.RaiseAsync( m, this, message );
            _eSeqMessage.Raise( m, this, message );
            return Task.WhenAll( task, _eSeqMessageAsync.RaiseAsync( m, this, message ) );
        }
        #endregion Events

        /// <summary>
        /// Check that the connection is still etablished.
        /// </summary>
        /// <param name="m">The monitor to use.</param>
        /// <returns>True if connected, false if not.</returns>
        public async ValueTask<bool> CheckConnectionAsync( IActivityMonitor m )
        {
            if( _isProtocolConnected && !Channel.IsConnected )
            {
                await CloseAsync( m, DisconnectedReason.RemoteDisconnected );
            }
            return _isProtocolConnected && Channel.IsConnected;
        }

        #region Disconnect

        public async Task DisconnectAsync( IActivityMonitor m )
        {
            using( m.OpenInfo( "Disconnecting..." ) )
            {
                if( !await CheckConnectionAsync( m ) ) return;
                try
                {
                    _packetsSubscription?.Dispose();

                    await Channel.SendAsync( new Mon<IPacket>( m, new Disconnect() ) );

                    await _packetListener
                        .PacketStream
                        .LastOrDefaultAsync();
                }
                finally
                {
                    await CloseAsync( m, DisconnectedReason.SelfDisconnected );
                }
            }
        }

        /// <summary>
        /// Close and call <see cref="RaiseDisconnectAsync(IActivityMonitor, MqttEndpointDisconnected)"/>
        /// </summary>
        /// <param name="m"></param>
        /// <param name="reason"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        Task CloseAsync( IActivityMonitor m, DisconnectedReason reason, string message = null )
        {
            using( m.OpenInfo( $"Client {ClientId} - Disconnecting: {reason.ToString()}" ) )
            {
                var disconnect = new MqttEndpointDisconnected( reason, message );
                Close( m );
                return RaiseDisconnectAsync( m, disconnect );
            }

        }

        Task CloseAsync( IActivityMonitor m, Exception e )
        {
            using( m.OpenError( e ) )
            {
                return CloseAsync( m, DisconnectedReason.Error, e.Message );
            }
        }

        /// <summary>
        /// Close the connection without calling <see cref="RaiseDisconnectAsync(IActivityMonitor, MqttEndpointDisconnected)"/>.
        /// </summary>
        /// <param name="m">The activity monitor.</param>
        /// <returns></returns>
        void Close( IActivityMonitor m )
        {
            using( m.OpenInfo( $"Client {ClientId} - Closing." ) )
            {
                CloseClientSession( m );
                _packetsSubscription?.Dispose();
                _packetListener?.Dispose();
                Channel?.Dispose();
                _isProtocolConnected = false;
                ClientId = null;
            }
        }

        #region Session
        void CloseClientSession( IActivityMonitor m )
        {
            ClientSession session = string.IsNullOrEmpty( ClientId ) ? default : _sessionRepository.Read( ClientId );

            if( session == null ) return;

            if( session.Clean )
            {
                _sessionRepository.Delete( session.Id );
                m.Info( ClientProperties.Client_DeletedSessionOnDisconnect( ClientId ) );
            }
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
        #endregion Session

        #endregion Disconnect

        #region Connect
        public async Task<SessionState> ConnectAsync( IActivityMonitor m, MqttClientCredentials credentials, MqttLastWill will = null, bool cleanSession = false )
        {
            async Task InitializeChannelAsync( IActivityMonitor m )
            {
                Channel = await _channelFactory
                    .CreateAsync( m );

                _packetListener = new ClientPacketListener( Channel, _flowProvider, _configuration );
                _packetListener.Listen();
                ObservePackets();
            }

            using( var grp = m.OpenInfo( $"Connecting to server..." ) )
            {
                try
                {
                    if( await CheckConnectionAsync( m ) ) throw new InvalidOperationException( $"The protocol connection cannot be performed because an active connection for client {ClientId} already exists" );
                    ClientId = string.IsNullOrEmpty( credentials.ClientId ) ?
                        MqttClientOld.GetAnonymousClientId() :
                        credentials.ClientId;
                    if( !cleanSession && string.IsNullOrEmpty( credentials?.ClientId ) ) throw new InvalidOperationException( ClientProperties.Client_AnonymousClientWithoutCleanSession );

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
                        await Channel.SendAsync( new Mon<IPacket>( m, connect ) );

                        TimeSpan connectTimeout = TimeSpan.FromSeconds( _configuration.WaitTimeoutSecs );
                        ConnectAck ack = (await _packetListener
                            .PacketStream
                            .OfMonitoredType<ConnectAck, IPacket>()
                            .FirstOrDefaultAsync()
                            .Timeout( connectTimeout )).Item;

                        if( ack == null ) throw new TimeoutException($"Timed out after { _configuration.WaitTimeoutSecs} seconds, was waiting ConnectAck.");

                        if( ack.Status != ConnectionStatus.Accepted ) throw new MqttConnectionException( ack.Status );

                        _isProtocolConnected = true;

                        return ack.SessionState ? SessionState.SessionPresent : SessionState.CleanSession;
                    }
                }
                catch( TimeoutException timeEx )
                {
                    await CloseAsync( m, timeEx );
                    throw;
                }
                catch( MqttConnectionException connectionEx )
                {
                    await CloseAsync( m, connectionEx );
                    //TODO: Exceptiong logic handing should not come from the type of the exception but by value returned by a function
                    string message = ClientProperties.Client_ConnectNotAccepted( ClientId, connectionEx.ReturnCode );

                    throw new MqttClientException( message, connectionEx );
                }
                catch( MqttClientException clientEx )
                {
                    await CloseAsync( m, clientEx );
                    throw;
                }
                catch( Exception ex )
                {
                    await CloseAsync( m, ex );
                    throw new MqttClientException( ClientProperties.Client_ConnectionError( ClientId ), ex );
                }
            }
        }

        public Task<SessionState> ConnectAnonymousAsync( IActivityMonitor m, MqttLastWill will = null ) =>
            ConnectAsync( m, new MqttClientCredentials(), will, cleanSession: true );

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
                        using( packet.Monitor.OpenInfo( ClientProperties.Client_NewApplicationMessageReceived( ClientId, publish.Topic ) ) )
                        {
                            RaiseMessageAsync( packet.Monitor, message ).Wait();
                        }
                    }
                }, ex =>
                {
                    var m = new ActivityMonitor();//TODO: Remove monitor in onerror.
                    CloseAsync( m, ex ).Wait();
                }, () =>
                {
                    var m = new ActivityMonitor();//TODO: Remove monitor in oncomplete.
                    m.Warn( ClientProperties.Client_PacketsObservableCompleted );
                    CloseAsync( m, DisconnectedReason.RemoteDisconnected ).Wait();
                } );
        }

        #endregion Connect

        async ValueTask EnsureConnected( IActivityMonitor m )
        {
            if( !await CheckConnectionAsync( m ) ) throw new InvalidOperationException( "Client is not connected to server." );
        }

        public async Task<SubscribeReturnCode[]> SubscribeAsync( IActivityMonitor m, string topicFilter, MqttQualityOfService qos )
        {
            await EnsureConnected( m );
            using( m.OpenInfo( $"Subscribing on topic {topicFilter} with qos {qos.ToString()}" ) )
            {
                try
                {
                    ushort packetId = _packetIdProvider.GetPacketId();
                    Subscribe subscribe = new Subscribe( packetId, new Subscription( topicFilter, qos ) );
                    await Channel.SendAsync( new Mon<IPacket>( m, subscribe ) );

                    TimeSpan subscribeTimeout = TimeSpan.FromSeconds( _configuration.WaitTimeoutSecs );

                    SubscribeAck ack = (await _packetListener
                        .PacketStream
                        .OfMonitoredType<SubscribeAck, IPacket>()
                        .FirstOrDefaultAsync( x => x.Item.PacketId == packetId )
                        .Timeout( subscribeTimeout )).Item;

                    if( ack == null ) throw new TimeoutException();
                    return ack.ReturnCodes;
                }
                catch( Exception e )
                {
                    await CloseAsync( m, e );
                    throw;
                }
            }
        }

        public async Task UnsubscribeAsync( IActivityMonitor m, IEnumerable<string> topics )
        {
            await EnsureConnected( m );
            try
            {
                topics ??= Array.Empty<string>();

                ushort packetId = _packetIdProvider.GetPacketId();
                Unsubscribe unsubscribe = new Unsubscribe( packetId, topics );
                await Channel.SendAsync( new Mon<IPacket>( m, unsubscribe ) );

                TimeSpan unsubscribeTimeout = TimeSpan.FromSeconds( _configuration.WaitTimeoutSecs );

                UnsubscribeAck ack = (await _packetListener
                    .PacketStream
                    .OfMonitoredType<UnsubscribeAck, IPacket>()
                    .FirstOrDefaultAsync( x => x.Item.PacketId == packetId )
                    .Timeout( unsubscribeTimeout )).Item;

                if( ack == null ) throw new TimeoutException();
            }
            catch( Exception ex )
            {
                await CloseAsync( m, ex );
                throw;
            }
        }

        public async Task PublishAsync( IActivityMonitor m, string topic, ReadOnlyMemory<byte> payload, MqttQualityOfService qos, bool retain = false )
        {
            await EnsureConnected( m );
            try
            {
                ushort? packetId = qos == MqttQualityOfService.AtMostOnce ? null : (ushort?)_packetIdProvider.GetPacketId();
                Publish publish = new Publish( topic, payload, qos, retain, duplicated: false, packetId: packetId );

                PublishSenderFlow senderFlow = _flowProvider.GetFlow<PublishSenderFlow>();
                await senderFlow.SendPublishAsync( m, ClientId, publish, Channel );
            }
            catch( Exception ex )
            {
                await CloseAsync( m, ex );
                throw;
            }
        }
    }
}
