using CK.Core;

using CK.MQTT.Sdk.Flows;
using CK.MQTT.Sdk.Packets;
using System;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk
{
    internal class ClientPacketListener : IPacketListener
    {
        readonly IActivityMonitor _m;
        readonly IMqttChannel<IPacket> _channel;
        readonly IProtocolFlowProvider _flowProvider;
        readonly MqttConfiguration _configuration;
        readonly ReplaySubject<Mon<IPacket>> _packets;
        readonly TaskRunner _flowRunner;
        IDisposable _listenerDisposable;
        bool _disposed;
        string _clientId = string.Empty;
        IDisposable _keepAliveMonitor;

        public ClientPacketListener( IActivityMonitor m,
            IMqttChannel<IPacket> channel,
            IProtocolFlowProvider flowProvider,
            MqttConfiguration configuration )
        {
            _m = m;
            _channel = channel;
            _flowProvider = flowProvider;
            _configuration = configuration;
            _packets = new ReplaySubject<Mon<IPacket>>( window: TimeSpan.FromSeconds( configuration.WaitTimeoutSecs ) );
            _flowRunner = TaskRunner.Get();
        }

        public IObservable<Mon<IPacket>> PacketStream => _packets;

        public void Listen()
        {
            if( _disposed )
            {
                throw new ObjectDisposedException( GetType().FullName );
            }

            _listenerDisposable = new CompositeDisposable(
                ListenFirstPacket(),
                ListenNextPackets(),
                ListenCompletionAndErrors(),
                ListenSentConnectPacket(),
                ListenSentDisconnectPacket() );
        }

        public void Dispose()
        {
            if( _disposed )
            {
                return;
            }

            _m.Info( ClientProperties.Mqtt_Disposing( GetType().FullName ) );

            _listenerDisposable.Dispose();
            StopKeepAliveMonitor();
            _packets.OnCompleted();
            (_flowRunner as IDisposable)?.Dispose();
            _disposed = true;
        }

        IDisposable ListenFirstPacket()
        {
            return _channel
                .ReceiverStream
                .FirstOrDefaultAsync()
                .Subscribe( async packet =>
                {
                    if( packet.Item == default( IPacket ) )
                    {
                        return;
                    }

                    packet.Monitor.Info( ClientProperties.ClientPacketListener_FirstPacketReceived( _clientId, packet.Item.Type ) );

                    if( !(packet.Item is ConnectAck connectAck) )
                    {
                        NotifyError( packet.Monitor, ClientProperties.ClientPacketListener_FirstReceivedPacketMustBeConnectAck );
                        return;
                    }

                    if( _configuration.KeepAliveSecs > 0 )
                    {
                        StartKeepAliveMonitor();
                    }

                    await DispatchPacketAsync( packet );
                }, ex =>
                {
                    var m = new ActivityMonitor();//TODO: Remove onerror monitor.
                    NotifyError( m, ex );
                } );
        }

        IDisposable ListenNextPackets()
            => _channel
                .ReceiverStream
                .Skip( 1 )
                .Subscribe(
                    async packet => await DispatchPacketAsync( packet )
                    , ex =>
                    {
                        var m = new ActivityMonitor();//TODO: Remove onerror monitor.
                        NotifyError( m, ex );
                    }
                );

        IDisposable ListenCompletionAndErrors()
            => _channel
                .ReceiverStream
                .Subscribe( _ => { },
                    ex =>
                    {
                        var m = new ActivityMonitor();//TODO: Remove onerror monitor.
                        NotifyError( m, ex );
                    }
                    , () =>
                    {
                        var m = new ActivityMonitor();//TODO: Remove oncomplete monitor.
                        m.Warn( ClientProperties.ClientPacketListener_PacketChannelCompleted( _clientId ) );
                        _packets.OnCompleted();
                    }
                );

        IDisposable ListenSentConnectPacket()
            => _channel
                .SenderStream
                .OfMonitoredType<Connect, IPacket>()
                .FirstAsync()
                .Subscribe( connect => _clientId = connect.Item.ClientId );

        IDisposable ListenSentDisconnectPacket()
            => _channel.SenderStream
                .OfMonitoredType<Disconnect, IPacket>()
                .FirstAsync()
                .Subscribe( disconnect =>
                {
                    if( _configuration.KeepAliveSecs > 0 ) StopKeepAliveMonitor();
                } );

        void StartKeepAliveMonitor()
        {
            var monitor = new ActivityMonitor();
            int interval = _configuration.KeepAliveSecs * 1000;
            _keepAliveMonitor = _channel.SenderStream
                .Buffer( new TimeSpan( 0, 0, _configuration.KeepAliveSecs ), 1 )
                .Where( p => p.Count == 0 )
                .Subscribe( p =>
                 {
                     try
                     {
                         monitor.Info( ClientProperties.ClientPacketListener_SendingKeepAlive( _clientId, _configuration.KeepAliveSecs ) );

                         PingRequest ping = new PingRequest();

                         _channel.SendAsync( new Mon<IPacket>( monitor, ping ) );
                         _channel.ReceiverStream
                             .OfMonitoredType<PingResponse, IPacket>()
                             .Timeout(TimeSpan.FromSeconds( _configuration.WaitTimeoutSecs ) )
                             .FirstOrDefaultAsync();
                     }
                     catch( Exception ex )
                     {
                         NotifyError( monitor, ex );
                     }
                 } );
        }

        void StopKeepAliveMonitor()
        {
            _keepAliveMonitor?.Dispose();
        }

        async Task DispatchPacketAsync( Mon<IPacket> packet )
        {
            IProtocolFlow flow = _flowProvider.GetFlow( packet.Item.Type );

            if( flow != null )
            {
                try
                {
                    using(packet.Monitor.OpenTrace("Emitting packet to packet stream."))
                    {
                        _packets.OnNext( packet );
                    }

                    await _flowRunner.Run( async () =>
                    {
                        Publish publish = packet.Item as Publish;
                        IDisposableGroup group;
                        if( publish == null )
                        {
                            group = packet.Monitor.OpenInfo( ClientProperties.ClientPacketListener_DispatchingMessage( _clientId, packet.Item.Type, flow.GetType().Name ) );
                        }
                        else
                        {
                            group = packet.Monitor.OpenInfo( ClientProperties.ClientPacketListener_DispatchingPublish( _clientId, flow.GetType().Name, publish.Topic ) );
                        }

                        await flow.ExecuteAsync( packet.Monitor, _clientId, packet.Item, _channel );
                        group.Dispose();
                    } );
                }
                catch( Exception ex )
                {
                    NotifyError( packet.Monitor, ex );
                }
            }
        }

        void NotifyError( IActivityMonitor m, Exception exception )
        {
            m.Error( ClientProperties.ClientPacketListener_Error, exception );

            _packets.OnError( exception );
        }

        void NotifyError( IActivityMonitor m, string message )
        {
            NotifyError( m, new MqttException( message ) );
        }
    }
}
