using CK.Core;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk.Bindings
{
    internal class TcpChannelListener : IListener<GenericChannel>
    {
        readonly MqttConfiguration _configuration;
        readonly TcpListener _listener;
        bool _disposed;

        public TcpChannelListener( MqttConfiguration configuration )
        {
            _configuration = configuration;
            _listener = new TcpListener( IPAddress.Any, _configuration.Port );
        }

        public async Task<GenericChannel> AcceptClientAsync(IActivityMonitor m)
            => new GenericChannel(new TcpChannelClient(await _listener.AcceptTcpClientAsync()),
				new PacketBuffer(),
				_configuration);

        public void Start() => _listener.Start();

        public void Stop() => _listener.Stop();

        public void Dispose()
        {
            if( _disposed ) return;
            _listener.Stop();
            _disposed = true;
        }
    }
}
