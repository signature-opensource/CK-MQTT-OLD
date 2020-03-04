using CK.Core;

using CK.MQTT.Sdk.Packets;
using System;
using System.Diagnostics;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk
{
    internal class PacketChannel : IMqttChannel<IPacket>
    {
        bool _disposed;
        readonly IActivityMonitor _m;
        readonly IMqttChannel<byte[]> _innerChannel;
        readonly IPacketManager _manager;
        readonly ReplaySubject<IMonitored<IPacket>> _receiver;
        readonly ReplaySubject<IMonitored<IPacket>> _sender;
        readonly IDisposable _subscription;

        public PacketChannel( IActivityMonitor m,
            IMqttChannel<byte[]> innerChannel,
            IPacketManager manager,
            MqttConfiguration configuration )
        {
            _m = m;
            _innerChannel = innerChannel;
            _manager = manager;

            _receiver = new ReplaySubject<IMonitored<IPacket>>( window: TimeSpan.FromSeconds( configuration.WaitTimeoutSecs ) );
            _sender = new ReplaySubject<IMonitored<IPacket>>( window: TimeSpan.FromSeconds( configuration.WaitTimeoutSecs ) );
            _subscription = innerChannel
                .ReceiverStream
                .Subscribe( async bytes =>
                {
                    try
                    {
                        IMonitored<IPacket> packet = await _manager.GetPacketAsync( bytes );

                        _receiver.OnNext( packet );
                    }
                    catch( MqttException ex )
                    {
                        _receiver.OnError( ex );
                    }
                }, onError: ex => _receiver.OnError( ex ), onCompleted: () => _receiver.OnCompleted() );
        }

        public bool IsConnected => _innerChannel != null && _innerChannel.IsConnected;

        public IObservable<IMonitored<IPacket>> ReceiverStream => _receiver;

        public IObservable<IMonitored<IPacket>> SenderStream => _sender;

        public async Task SendAsync( IMonitored<IPacket> packet )
        {
            if( _disposed )
            {
                throw new ObjectDisposedException( GetType().FullName );
            }
            var bytes = await _manager.GetBytesAsync( packet );

            _sender.OnNext( packet );

            await _innerChannel.SendAsync( bytes );
        }

        public void Dispose()
        {
            if( _disposed ) return;

            _m.Info( ClientProperties.Mqtt_Disposing( GetType().FullName ) );

            _subscription.Dispose();
            _receiver.OnCompleted();
            _innerChannel.Dispose();
            _disposed = true;
        }
    }
}
