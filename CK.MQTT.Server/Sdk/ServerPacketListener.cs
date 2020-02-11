using CK.MQTT.Sdk.Flows;
using CK.MQTT.Sdk.Packets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk
{
    internal class ServerPacketListener : IPacketListener
    {
        static readonly ITracer _tracer = Tracer.Get<ServerPacketListener>();

        readonly IMqttChannel<IPacket> _channel;
        readonly IConnectionProvider _connectionProvider;
        readonly IProtocolFlowProvider _flowProvider;
        readonly MqttConfiguration _configuration;
        readonly ReplaySubject<IPacket> _packets;
        readonly TaskRunner _flowRunner;
        CompositeDisposable _listenerDisposable;
        bool _disposed;
        string _clientId = string.Empty;
        int _keepAlive = 0;

        public ServerPacketListener( IMqttChannel<IPacket> channel,
            IConnectionProvider connectionProvider,
            IProtocolFlowProvider flowProvider,
            MqttConfiguration configuration )
        {
            _channel = channel;
            _connectionProvider = connectionProvider;
            _flowProvider = flowProvider;
            _configuration = configuration;
            _packets = new ReplaySubject<IPacket>( window: TimeSpan.FromSeconds( configuration.WaitTimeoutSecs ) );
            _flowRunner = TaskRunner.Get();
        }

        public IObservable<IPacket> PacketStream => _packets;

        public void Listen()
        {
            if( _disposed ) throw new ObjectDisposedException( GetType().FullName );

            _listenerDisposable = new CompositeDisposable(
                ListenFirstPacket(),
                ListenNextPackets(),
                ListenCompletionAndErrors(),
                ListenSentPackets() );
        }

        public void Dispose()
        {
            if( _disposed ) return;

            _tracer.Info( ServerProperties.Resources.GetString( "Mqtt_Disposing" ), GetType().FullName );

            _listenerDisposable.Dispose();
            _packets.OnCompleted();
            (_flowRunner as IDisposable)?.Dispose();
            _disposed = true;
        }

        IDisposable ListenFirstPacket()
        {
            TimeSpan packetDueTime = TimeSpan.FromSeconds( _configuration.WaitTimeoutSecs );

            return _channel
                .ReceiverStream
                .FirstOrDefaultAsync()
                .Timeout( packetDueTime )
                .Subscribe( async packet =>
                {
                    if( packet == default( IPacket ) )
                    {
                        return;
                    }

                    Connect connect = packet as Connect;

                    if( connect == null )
                    {
                        await NotifyErrorAsync( ServerProperties.Resources.GetString( "ServerPacketListener_FirstPacketMustBeConnect" ) );

                        return;
                    }

                    _clientId = connect.ClientId;
                    _keepAlive = connect.KeepAlive;
                    _connectionProvider.AddConnection( _clientId, _channel );

                    _tracer.Info( ServerProperties.Resources.GetString( "ServerPacketListener_ConnectPacketReceived" ), _clientId );

                    await DispatchPacketAsync( connect );
                }, async ex =>
                {
                    await HandleConnectionExceptionAsync( ex );
                } );
        }

        IDisposable ListenNextPackets()
            => _channel
                .ReceiverStream
                .Skip( 1 )
                .Subscribe( async packet =>
                {
                    if( packet is Connect )
                    {
                        await NotifyErrorAsync( new MqttProtocolViolationException( ServerProperties.Resources.GetString( "ServerPacketListener_SecondConnectNotAllowed" ) ) );

                        return;
                    }

                    await DispatchPacketAsync( packet );
                }, async ex =>
                {
                    await NotifyErrorAsync( ex );
                } );

        IDisposable ListenCompletionAndErrors()
            => _channel
                .ReceiverStream
                .Subscribe( _ => { },
                    async ex =>
                    {
                        await NotifyErrorAsync( ex );
                    }, async () =>
                    {
                        await SendLastWillAsync();
                        CompletePacketStream();
                    }
                );

        IDisposable ListenSentPackets()
            => _channel.SenderStream
                .OfType<ConnectAck>()
                .FirstAsync()
                .Subscribe( connectAck =>
                {
                    if( _keepAlive > 0 )
                    {
                        MonitorKeepAliveAsync();
                    }
                } );

        async Task HandleConnectionExceptionAsync( Exception exception )
        {
            if( exception is TimeoutException )
            {
                await NotifyErrorAsync( ServerProperties.Resources.GetString( "ServerPacketListener_NoConnectReceived" ), exception );
            }
            else if( exception is MqttConnectionException )
            {
                _tracer.Error( exception, ServerProperties.Resources.GetString( "ServerPacketListener_ConnectionError" ), _clientId ?? "N/A" );

                MqttConnectionException connectEx = exception as MqttConnectionException;
                ConnectAck errorAck = new ConnectAck( connectEx.ReturnCode, existingSession: false );

                try
                {
                    await _channel.SendAsync( errorAck );
                }
                catch( Exception ex )
                {
                    await NotifyErrorAsync( ex );
                }
            }
            else
            {
                await NotifyErrorAsync( exception );
            }
        }

        void MonitorKeepAliveAsync()
        {
            TimeSpan tolerance = GetKeepAliveTolerance();

            IDisposable keepAliveSubscription = _channel
                .ReceiverStream
                .Timeout( tolerance )
                .Subscribe( _ => { }, async ex =>
                {
                    if( !(ex is TimeoutException timeEx) )
                    {
                        await NotifyErrorAsync( ex );
                    }
                    else
                    {
                        string message = string.Format( ServerProperties.Resources.GetString( "ServerPacketListener_KeepAliveTimeExceeded" ), tolerance, _clientId );

                        await NotifyErrorAsync( message, timeEx );
                    }
                } );

            _listenerDisposable.Add( keepAliveSubscription );
        }

        TimeSpan GetKeepAliveTolerance()
        {
            int tolerance = (int)Math.Round( _keepAlive * 1.5, MidpointRounding.AwayFromZero );

            return TimeSpan.FromSeconds( tolerance );
        }

        async Task DispatchPacketAsync( IPacket packet )
        {
            IProtocolFlow flow = _flowProvider.GetFlow( packet.Type );

            if( flow == null )
            {
                return;
            }

            try
            {
                _packets.OnNext( packet );

                await _flowRunner.Run( async () =>
                {
                    if( packet.Type == MqttPacketType.Publish )
                    {
                        Publish publish = packet as Publish;

                        _tracer.Info( ServerProperties.Resources.GetString( "ServerPacketListener_DispatchingPublish" ), flow.GetType().Name, _clientId, publish.Topic );
                    }
                    else if( packet.Type == MqttPacketType.Subscribe )
                    {
                        Subscribe subscribe = packet as Subscribe;
                        IEnumerable<string> topics = subscribe.Subscriptions == null ? new List<string>() : subscribe.Subscriptions.Select( s => s.TopicFilter );

                        _tracer.Info( ServerProperties.Resources.GetString( "ServerPacketListener_DispatchingSubscribe" ), flow.GetType().Name, _clientId, string.Join( ", ", topics ) );
                    }
                    else
                    {
                        _tracer.Info( ServerProperties.Resources.GetString( "ServerPacketListener_DispatchingMessage" ), packet.Type, flow.GetType().Name, _clientId );
                    }

                    await flow.ExecuteAsync( _clientId, packet, _channel );
                } );
            }
            catch( Exception ex )
            {
                if( flow is ServerConnectFlow )
                {
                    HandleConnectionExceptionAsync( ex ).Wait();
                }
                else
                {
                    await NotifyErrorAsync( ex );
                }
            }
        }

        async Task NotifyErrorAsync( Exception exception )
        {
            _tracer.Error( exception, ServerProperties.Resources.GetString( "ServerPacketListener_Error" ), _clientId ?? "N/A" );

            _listenerDisposable.Dispose();
            RemoveClient();
            await SendLastWillAsync();
            _packets.OnError( exception );
            CompletePacketStream();
        }

        Task NotifyErrorAsync( string message )
        {
            return NotifyErrorAsync( new MqttException( message ) );
        }

        Task NotifyErrorAsync( string message, Exception exception )
        {
            return NotifyErrorAsync( new MqttException( message, exception ) );
        }

        async Task SendLastWillAsync()
        {
            if( string.IsNullOrEmpty( _clientId ) ) return;

            IServerPublishReceiverFlow publishFlow = _flowProvider.GetFlow<IServerPublishReceiverFlow>();

            await publishFlow
                .SendWillAsync( _clientId );
        }

        void RemoveClient()
        {
            if( string.IsNullOrEmpty( _clientId ) ) return;

            _connectionProvider.RemoveConnection( _clientId );
        }

        void CompletePacketStream()
        {
            if( !string.IsNullOrEmpty( _clientId ) ) RemoveClient();

            _tracer.Warn( ServerProperties.Resources.GetString( "PacketChannelCompleted" ), _clientId ?? "N/A" );

            _packets.OnCompleted();
        }
    }
}
