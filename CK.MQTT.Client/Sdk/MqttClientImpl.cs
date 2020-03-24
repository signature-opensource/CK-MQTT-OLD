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
    internal class MqttClientImpl : IMqttClient
    {
        readonly MqttConfiguration _configuration;
        readonly IPacketChannelFactory _channelFactory;
        readonly IRepository<ClientSession> _sessionRepository;
        readonly IProtocolFlowProvider _flowProvider;
        readonly IPacketIdProvider _packetIdProvider;

        bool _isProtocolConnected;
        IPacketListener _packetListener;
        IDisposable _packetsSubscription;

        internal IMqttChannel<IPacket> Channel { get; private set; }
        public string ClientId { get; private set; }


        #region Events


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
        public bool CheckConnection( IActivityMonitor m )
        {
            if( _isProtocolConnected && !Channel.IsConnected )
            {
                Close( m );
            }
            return _isProtocolConnected && Channel.IsConnected;
        }

        #region Disconnect
        public async Task DisconnectAsync( IActivityMonitor m )
        {
            using( m.OpenInfo( "Disconnecting..." ) )
            {
                try
                {
                    if( !CheckConnection( m ) ) return;
                    _packetsSubscription?.Dispose();

                    await Channel.SendAsync( new Mon<IPacket>( m, new Disconnect() ) );
                    await _packetListener.PacketStream.LastOrDefaultAsync();
                }
                catch( Exception ex )
                {
                    m.Error( "Error while disconnecting.", ex );
                }
                await CloseAsync( m, DisconnectedReason.SelfDisconnected );
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
                    if( CheckConnection( m ) ) throw new MqttClientException( ClientProperties.Client_AlreadyConnected( ClientId ) );
                    if( !cleanSession && string.IsNullOrEmpty( credentials?.ClientId ) ) throw new MqttClientException( ClientProperties.Client_AnonymousClientWithoutCleanSession );
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
                        await Channel.SendAsync( new Mon<IPacket>( m, connect ) );

                        TimeSpan connectTimeout = TimeSpan.FromSeconds( _configuration.WaitTimeoutSecs );
                        ConnectAck ack = (await _packetListener
                            .PacketStream
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
                    await CloseAsync( m, timeEx );
                    throw new MqttClientException( ClientProperties.Client_ConnectionTimeout( ClientId ), timeEx );
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
                    CloseAsync( m, ex ).Wait();
                }, () =>
                {
                    var m = new ActivityMonitor();//TODO: Remove monitor in oncomplete.
                    m.Warn( ClientProperties.Client_PacketsObservableCompleted );
                    CloseAsync( m, DisconnectedReason.RemoteDisconnected ).Wait();
                } );
        }

        #endregion Connect

        public async Task<SubscribeReturnCode[]> SubscribeAsync( IActivityMonitor m, string topicFilter, MqttQualityOfService qos )
        {
            using( m.OpenInfo( $"Subscribing on topic {topicFilter} with qos {qos.ToString()}" ) )
            {
                if( !CheckConnection( m ) ) throw new InvalidOperationException( "Client is not connected to server." );
                try
                {
                    ushort packetId = _packetIdProvider.GetPacketId();
                    Subscribe subscribe = new Subscribe( packetId, new Subscription( topicFilter, qos ) );
                    await Channel.SendAsync( new Mon<IPacket>( m, subscribe ) );

                    TimeSpan subscribeTimeout = TimeSpan.FromSeconds( _configuration.WaitTimeoutSecs );

                    SubscribeAck ack = (await _packetListener
                        .PacketStream
                        .ObserveOn( NewThreadScheduler.Default )
                        .OfMonitoredType<SubscribeAck, IPacket>()
                        .FirstOrDefaultAsync( x => x.Item.PacketId == packetId )
                        .Timeout( subscribeTimeout )).Item;

                    if( ack == null ) throw new TimeoutException( $"Client {ClientId} - Timeout while waiting for Subscribe Ack." );
                    return ack.ReturnCodes;
                }
                catch( Exception e )
                {
                    await CloseAsync( m, e );
                    throw;
                }
            }
        }


    }
}
