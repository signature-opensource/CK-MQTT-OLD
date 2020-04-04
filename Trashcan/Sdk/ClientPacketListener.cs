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
        readonly IMqttChannel<IPacket> _channel;
        readonly IProtocolFlowProvider _flowProvider;
        readonly MqttConfiguration _configuration;
        readonly ReplaySubject<Mon<IPacket>> _packets;
        IDisposable _listenerDisposable;
        bool _disposed;
        string _clientId = string.Empty;
        IDisposable _keepAliveMonitor;

        public ClientPacketListener(
            IMqttChannel<IPacket> channel,
            IProtocolFlowProvider flowProvider,
            MqttConfiguration configuration )
        {
            _channel = channel;
            _flowProvider = flowProvider;
            _configuration = configuration;
            _packets = new ReplaySubject<Mon<IPacket>>( window: TimeSpan.FromSeconds( configuration.WaitTimeoutSecs ) );
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
            if( _disposed ) return;
            _listenerDisposable.Dispose();
            StopKeepAliveMonitor();
            _packets.OnCompleted();
            _disposed = true;
        }

        IDisposable ListenFirstPacket()
        {
            return _channel
                .ReceiverStream
                .FirstOrDefaultAsync()
                .Subscribe( packet =>
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

                    DispatchPacketAsync( packet ).Wait();
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
                    packet => DispatchPacketAsync( packet ).Wait()
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
                         if( _disposed )
                         {
                             monitor.Warn( "Keep Alive monitor was still running but the observable chain was disposed." );
                             return;
                         }
                         monitor.Info( $"Client '{_clientId}' - No packet has been sent in {_configuration.KeepAliveSecs} seconds. Sending Ping to Server to maintain Keep Alive" );

                         PingRequest ping = new PingRequest();

                         _channel.SendAsync( new Mon<IPacket>( monitor, ping ) );
                         _channel.ReceiverStream
                             .OfMonitoredType<PingResponse, IPacket>()
                             .Timeout( TimeSpan.FromSeconds( _configuration.WaitTimeoutSecs ) )
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
                    using( packet.Monitor.OpenTrace( $"Emitting packet to packet {packet.Item.Type} stream." ) )
                    {
                        _packets.OnNext( packet );
                    }

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
                }
                catch( Exception ex )
                {
                    NotifyError( packet.Monitor, ex );
                }
            }
            else
            {
                throw new InvalidOperationException( "Do we enter this branch path?" );
            }
        }

        void NotifyError( IActivityMonitor m, Exception exception )
        {
            m.Error( ClientProperties.ClientPacketListener_Error, exception );

            _packets.OnError( exception );
            StopKeepAliveMonitor();
        }

        void NotifyError( IActivityMonitor m, string message )
        {
            NotifyError( m, new MqttException( message ) );
        }
    }
}
