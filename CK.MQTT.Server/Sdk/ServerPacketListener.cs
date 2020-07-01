using CK.Core;

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
        readonly IActivityMonitor _m;
        readonly IMqttChannel<IPacket> _channel;
        readonly IConnectionProvider _connectionProvider;
        readonly IProtocolFlowProvider _flowProvider;
        readonly MqttConfiguration _configuration;
        readonly ReplaySubject<Mon<IPacket>> _packets;
        readonly TaskRunner _flowRunner;
        CompositeDisposable _listenerDisposable;
        bool _disposed;
        string _clientId = string.Empty;
        int _keepAlive = 0;

        public ServerPacketListener( IActivityMonitor m,
            IMqttChannel<IPacket> channel,
            IConnectionProvider connectionProvider,
            IProtocolFlowProvider flowProvider,
            MqttConfiguration configuration )
        {
            _m = m;
            _channel = channel;
            _connectionProvider = connectionProvider;
            _flowProvider = flowProvider;
            _configuration = configuration;
            _packets = new ReplaySubject<Mon<IPacket>>( window: TimeSpan.FromSeconds( configuration.WaitTimeoutSecs ) );
            _flowRunner = TaskRunner.Get();
        }

        public IObservable<Mon<IPacket>> PacketStream => _packets;

        public void Listen()
        {
            if( _disposed ) throw new ObjectDisposedException( GetType().FullName );

            _listenerDisposable = new CompositeDisposable();
            _listenerDisposable.Add( ListenFirstPacket() );
            _listenerDisposable.Add( ListenNextPackets() );
            _listenerDisposable.Add( ListenCompletionAndErrors() );
            _listenerDisposable.Add( ListenSentPackets() );
        }

        public void Dispose()
        {
            if( _disposed ) return;
            _disposed = true;
            _m.Info( ClientProperties.Mqtt_Disposing( GetType().FullName ) );

            _listenerDisposable.Dispose();
            _packets.OnCompleted();
            (_flowRunner as IDisposable)?.Dispose();
            _channel.Dispose();
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
                    if( packet.Item == default( IPacket ) )
                    {
                        return;
                    }

                    Connect connect = packet.Item as Connect;

                    if( connect == null )
                    {
                        await NotifyErrorAsync( packet.Monitor, ServerProperties.ServerPacketListener_FirstPacketMustBeConnect );
                        return;
                    }

                    _clientId = connect.ClientId;
                    _keepAlive = connect.KeepAlive;
                    _connectionProvider.AddConnection( packet.Monitor, _clientId, _channel );

                    packet.Monitor.Info( ServerProperties.ServerPacketListener_ConnectPacketReceived( _clientId ) );

                    await DispatchPacketAsync( new Mon<IPacket>( packet.Monitor, connect ) );
                }, async ( e ) =>
                {
                    var m = new ActivityMonitor();
                    await HandleConnectionExceptionAsync( m, e );
                } );
        }

        IDisposable ListenNextPackets()
            => _channel
                .ReceiverStream
                .Skip( 1 )
                .Subscribe( async packet =>
                {
                    if( packet.Item is Connect )
                    {
                        await NotifyErrorAsync( packet.Monitor, new MqttProtocolViolationException( ServerProperties.ServerPacketListener_SecondConnectNotAllowed ) );

                        return;
                    }

                    await DispatchPacketAsync( packet );
                }, async ex =>
                {
                    var m = new ActivityMonitor();//TODO: avoid creating a new Monitor.
                    await NotifyErrorAsync( m, ex );
                } );

        IDisposable ListenCompletionAndErrors()
            => _channel
                .ReceiverStream
                .Subscribe( _ => { },
                    async ex =>
                    {
                        var m = new ActivityMonitor();//TODO: avoid creating a new Monitor.
                        await NotifyErrorAsync( m, ex );
                    }, async () =>
                    {
                        var m = new ActivityMonitor();//TODO: avoid creating a new monitor in the OnComplete.
                        await SendLastWillAsync( m );
                        CompletePacketStream( m );
                    }
                );

        IDisposable ListenSentPackets()
            => _channel.SenderStream
                .OfMonitoredType<ConnectAck, IPacket>()
                .FirstAsync()
                .Subscribe( connectAck =>
                {
                    if( _keepAlive > 0 )
                    {
                        MonitorKeepAliveAsync();
                    }
                } );

        async Task HandleConnectionExceptionAsync( IActivityMonitor m, Exception exception )
        {
            if( exception is TimeoutException )
            {
                await NotifyErrorAsync( m, ServerProperties.ServerPacketListener_NoConnectReceived, exception );
            }
            else if( exception is MqttConnectionException )
            {
                m.Error( ServerProperties.ServerPacketListener_ConnectionError( _clientId ?? "N/A" ), exception );

                MqttConnectionException connectEx = exception as MqttConnectionException;
                ConnectAck errorAck = new ConnectAck( connectEx.ReturnCode, existingSession: false );

                try
                {
                    await _channel.SendAsync( new Mon<IPacket>( m, errorAck ) );
                }
                catch( Exception ex )
                {
                    await NotifyErrorAsync( m, ex );
                }
            }
            else
            {
                await NotifyErrorAsync( m, exception );
            }
        }

        void MonitorKeepAliveAsync()
        {
            TimeSpan tolerance = GetKeepAliveTolerance();
            var m = new ActivityMonitor();//TODO: avoid creating a new Monitor. 
            m.Trace( $"Starting to monitor client '{_clientId}' keep alives." );
            IDisposable keepAliveSubscription = _channel
                .ReceiverStream
                .Timeout( tolerance )
                .Subscribe( _ => { }, async ex =>
                {
                    if( !(ex is TimeoutException timeEx) )
                    {
                        await NotifyErrorAsync( m, ex );
                    }
                    else
                    {
                        string message = ServerProperties.ServerPacketListener_KeepAliveTimeExceeded( tolerance, _clientId );
                        m.Info( message );
                        await StopListening( m );
                    }
                } );

            _listenerDisposable.Add( keepAliveSubscription );
        }

        TimeSpan GetKeepAliveTolerance()
        {
            int tolerance = (int)Math.Round( _keepAlive * 1.5, MidpointRounding.AwayFromZero );

            return TimeSpan.FromSeconds( tolerance );
        }

        async Task DispatchPacketAsync( Mon<IPacket> packet )
        {
            IProtocolFlow flow = _flowProvider.GetFlow( packet.Item.Type );

            if( flow == null )
            {
                return;
            }

            try
            {
                using(packet.Monitor.OpenTrace("Dispatching packet."))
                {
                    _packets.OnNext( packet );
                }

                await _flowRunner.Run( async () =>
                {
                    IDisposableGroup diposeGroup;
                    if( packet.Item.Type == MqttPacketType.Publish )
                    {
                        Publish publish = packet.Item as Publish;

                        diposeGroup = packet.Monitor.OpenInfo( ServerProperties.ServerPacketListener_DispatchingPublish( flow.GetType().Name, _clientId, publish.Topic ) );
                    }
                    else if( packet.Item.Type == MqttPacketType.Subscribe )
                    {
                        Subscribe subscribe = packet.Item as Subscribe;
                        IEnumerable<string> topics = subscribe.Subscriptions == null ? new List<string>() : subscribe.Subscriptions.Select( s => s.TopicFilter );

                        diposeGroup = packet.Monitor.OpenInfo( ServerProperties.ServerPacketListener_DispatchingSubscribe( flow.GetType().Name, _clientId, string.Join( ", ", topics ) ) );
                    }
                    else
                    {
                        diposeGroup = packet.Monitor.OpenInfo( ServerProperties.ServerPacketListener_DispatchingMessage( packet.Item.Type, flow.GetType().Name, _clientId ) );
                    }

                    await flow.ExecuteAsync( packet.Monitor, _clientId, packet.Item, _channel );
                    diposeGroup.Dispose();
                } );
            }
            catch( Exception ex )
            {
                if( flow is ServerConnectFlow )
                {
                    HandleConnectionExceptionAsync( packet.Monitor, ex ).Wait();
                }
                else
                {
                    await NotifyErrorAsync( packet.Monitor, ex );
                }
            }
        }

        async Task StopListening( IActivityMonitor m )
        {
            using( m.OpenTrace( "Stopping to listen." ) )
            {
                _listenerDisposable.Dispose();
                RemoveClient( m );
                await SendLastWillAsync( m );
                CompletePacketStream( m );
            }
        }

        async Task NotifyErrorAsync( IActivityMonitor m, Exception exception )
        {
            m.Error( ServerProperties.ServerPacketListener_Error( _clientId ?? "N/A" ), exception );

            _listenerDisposable.Dispose();
            RemoveClient( m );
            await SendLastWillAsync( m );
            _packets.OnError( exception );
            CompletePacketStream( m );
        }

        Task NotifyErrorAsync( IActivityMonitor m, string message )
        {
            return NotifyErrorAsync( m, new MqttException( message ) );
        }

        Task NotifyErrorAsync( IActivityMonitor m, string message, Exception exception )
        {
            return NotifyErrorAsync( m, new MqttException( message, exception ) );
        }

        async Task SendLastWillAsync( IActivityMonitor m )
        {
            if( string.IsNullOrEmpty( _clientId ) ) return;

            IServerPublishReceiverFlow publishFlow = _flowProvider.GetFlow<IServerPublishReceiverFlow>();

            await publishFlow
                .SendWillAsync( m, _clientId );
        }

        void RemoveClient( IActivityMonitor m )
        {
            if( string.IsNullOrEmpty( _clientId ) ) return;

            _connectionProvider.RemoveConnection( m, _clientId );
        }

        void CompletePacketStream( IActivityMonitor m )
        {
            if( !string.IsNullOrEmpty( _clientId ) ) RemoveClient( m );

            m.Warn( ServerProperties.PacketChannelCompleted( _clientId ?? "N/A" ) );

            _packets.OnCompleted();
        }
    }
}
