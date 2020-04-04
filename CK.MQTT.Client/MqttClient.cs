using CK.Core;
using CK.MQTT.Client.Processes;
using CK.MQTT.Packets;
using CK.MQTT.Sdk;
using CK.MQTT.Sdk.Packets;
using CK.MQTT.Sdk.Storage;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CK.MQTT.Client.Sdk
{
    internal class MqttClient : IMqttClient
    {
        readonly IPacketChannelFactory _channelFactory;
        readonly IRepository<ClientSession> _sessionRepository;
        readonly MqttConfiguration _configuration;
        readonly IPacketIdProvider _packetIdProvider;

        public MqttClient(
            IPacketChannelFactory channelFactory,
            IRepositoryProvider repositoryProvider,
            IPacketIdProvider packetIdProvider,
            MqttConfiguration configuration )
        {
            _channelFactory = channelFactory;
            _packetIdProvider = packetIdProvider;
            _sessionRepository = repositoryProvider.GetRepository<ClientSession>();
            _configuration = configuration;
        }

        /// <summary>
        /// The ClientId of the <see cref="MqttClient"/>. <see cref="null"/> until connected.
        /// </summary>
        public string? ClientId { get; private set; }

        /// <summary>
        /// <see cref="null"/> until connected
        /// </summary>
        internal IMqttChannel<IPacket>? Channel { get; private set; }

        #region Events
        readonly SequentialEventHandlerSender<IMqttClient, MqttEndpointDisconnected> _eSeqDisconnect = new SequentialEventHandlerSender<IMqttClient, MqttEndpointDisconnected>();
        public event SequentialEventHandler<IMqttClient, MqttEndpointDisconnected> Disconnected
        {
            add => _eSeqDisconnect.Add( value );
            remove => _eSeqDisconnect.Remove( value );
        }

        readonly SequentialEventHandlerAsyncSender<IMqttClient, MqttEndpointDisconnected> _eSeqDisconnectAsync = new SequentialEventHandlerAsyncSender<IMqttClient, MqttEndpointDisconnected>();
        public event SequentialEventHandlerAsync<IMqttClient, MqttEndpointDisconnected> DisconnectedAsync
        {
            add => _eSeqDisconnectAsync.Add( value );
            remove => _eSeqDisconnectAsync.Remove( value );
        }

        readonly ParallelEventHandlerAsyncSender<IMqttClient, MqttEndpointDisconnected> _eParDisconnectAsync = new ParallelEventHandlerAsyncSender<IMqttClient, MqttEndpointDisconnected>();
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

        readonly ParallelEventHandlerAsyncSender<IMqttClient, ApplicationMessage> _eParMessageAsync = new ParallelEventHandlerAsyncSender<IMqttClient, ApplicationMessage>();
        public event ParallelEventHandlerAsync<IMqttClient, ApplicationMessage> ParallelMessageReceivedAsync
        {
            add => _eParMessageAsync.Add( value );
            remove => _eParMessageAsync.Remove( value );
        }

        readonly SequentialEventHandlerSender<IMqttClient, ApplicationMessage> _eSeqMessage = new SequentialEventHandlerSender<IMqttClient, ApplicationMessage>();
        public event SequentialEventHandler<IMqttClient, ApplicationMessage> MessageReceived
        {
            add => _eSeqMessage.Add( value );
            remove => _eSeqMessage.Remove( value );
        }

        public Task<ApplicationMessage?> WaitMessageReceivedAsync( Func<ApplicationMessage, bool>? predicate = null, int timeoutMillisecond = -1 )
            => _eSeqMessage.WaitAsync( predicate, timeoutMillisecond );

        readonly SequentialEventHandlerAsyncSender<IMqttClient, ApplicationMessage> _eSeqMessageAsync = new SequentialEventHandlerAsyncSender<IMqttClient, ApplicationMessage>();
        public event SequentialEventHandlerAsync<IMqttClient, ApplicationMessage> MessageReceivedAsync
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
        Task RaiseMessageAsync( IActivityMonitor m, ApplicationMessage message )
        {
            Task task = _eParMessageAsync.RaiseAsync( m, this, message );
            _eSeqMessage.Raise( m, this, message );
            return Task.WhenAll( task, _eSeqMessageAsync.RaiseAsync( m, this, message ) );
        }
        #endregion Events

        #region Session Helpers
        void CloseClientSession( IActivityMonitor m )
        {
            if( string.IsNullOrEmpty( ClientId ) ) return;
            ClientSession? session = _sessionRepository.Read( ClientId );
            if( session == null ) return;

            if( session.Clean )
            {
                _sessionRepository.Delete( session.Id );
                m.Info( ClientProperties.Client_DeletedSessionOnDisconnect( ClientId ) );
            }
        }

        void OpenClientSession( IActivityMonitor m, bool cleanSession )
        {
            if( ClientId == null ) throw new NullReferenceException( nameof( ClientId ) + " should not be null." );
            ClientSession? session = string.IsNullOrEmpty( ClientId ) ? null : _sessionRepository.Read( ClientId );

            if( cleanSession && session != null )
            {
                _sessionRepository.Delete( session.Id );
                session = null;

                m.Info( $"Client {ClientId} - Cleaned old session" );
            }

            if( session == null )
            {
                session = new ClientSession( ClientId, cleanSession );

                _sessionRepository.Create( session );

                m.Info( ClientProperties.Client_CreatedSession( ClientId ) );
            }
        }
        #endregion Session Helpers

        #region Connection

        bool _isProtocolConnected;
        public async ValueTask<bool> CheckConnectionAsync( IActivityMonitor m )
        {
            if( _isProtocolConnected && !(Channel?.IsConnected ?? false) )
            {
                await CloseAsync( m, DisconnectedReason.RemoteDisconnected );
            }
            return _isProtocolConnected && (Channel?.IsConnected ?? false);
        }


        Task CloseAsync( IActivityMonitor m, Exception e )
        {
            using( m.OpenError( e ) )
            {
                return CloseAsync( m, DisconnectedReason.Error, e.Message );
            }
        }

        Task CloseAsync( IActivityMonitor m, DisconnectedReason reason, string? message = null )
        {
            using( m.OpenInfo( $"Client {ClientId} - Disconnecting: {reason}" ) )
            {
                var disconnect = new MqttEndpointDisconnected( reason, message );
                using( m.OpenInfo( $"Client {ClientId} - Closing." ) )
                {
                    CloseClientSession( m );
                    Channel?.Close( m );
                    Channel?.Dispose();
                    _isProtocolConnected = false;
                    ClientId = null;
                }
                return RaiseDisconnectAsync( m, disconnect );
            }
        }

        public Task<ConnectResult> ConnectAnonymousAsync( IActivityMonitor m, MqttLastWill? will = null )
            => ConnectAsync( m, new MqttClientCredentials(), will, cleanSession: true );

        static string GetAnonymousClientId()
            => "anonymous" + Guid.NewGuid().ToString().Replace( "-", "" ).Substring( 0, 10 );

        public async Task<ConnectResult> ConnectAsync( IActivityMonitor m, MqttClientCredentials credentials, MqttLastWill? will = null, bool cleanSession = false )
        {
            if( await CheckConnectionAsync( m ) ) throw new InvalidOperationException( $"The protocol connection cannot be performed because an active connection for client {ClientId} already exists" );
            ClientId = string.IsNullOrEmpty( credentials.ClientId ) ?
                        GetAnonymousClientId() :
                        credentials.ClientId;
            using( m.OpenInfo( $"Connecting to server with client id '{ClientId}'." ) )
            {
                try
                {
                    OpenClientSession( m, cleanSession );
                    Channel = await _channelFactory.CreateAsync( m );
                    return await ConnectProcess.ExecuteConnectProtocol(
                        m, Channel, ClientId, cleanSession, MqttProtocol.SupportedLevel,
                        credentials.UserName, credentials.Password, will,
                        _configuration.KeepAliveSecs, _configuration.WaitTimeoutSecs );
                }
                catch( Exception e )
                {
                    await CloseAsync( m, e );
                    throw;
                }
            }
        }

        public async Task DisconnectAsync( IActivityMonitor m )
        {
            using( m.OpenInfo( "Disconnecting..." ) )
            {
                if( !await CheckConnectionAsync( m ) ) return;
                try
                {
                    if( Channel == null )
                    {
                        m.Warn( "Channel was null when disconnecting." );
                        return;
                    }
                    await Channel.Close( m );
                }
                finally
                {
                    await CloseAsync( m, DisconnectedReason.SelfDisconnected );
                }
            }
        }

        async ValueTask EnsureConnected( IActivityMonitor m )
        {
            if( !await CheckConnectionAsync( m ) ) throw new InvalidOperationException( "Client is not connected to server." );
        }
        #endregion Connection

        public async Task PublishAsync( IActivityMonitor m, string topic, ReadOnlyMemory<byte> payload, QualityOfService qos, bool retain = false )
        {
            await EnsureConnected( m );
        }

        public async Task<SubscribeReturnCode[]> SubscribeAsync( IActivityMonitor m, string topicFilter, QualityOfService qos )
        {
            await EnsureConnected( m );

            throw new NotImplementedException();
        }

        public async Task UnsubscribeAsync( IActivityMonitor m, IEnumerable<string> topics )
        {
            await EnsureConnected( m );

            throw new NotImplementedException();
        }
    }
}
