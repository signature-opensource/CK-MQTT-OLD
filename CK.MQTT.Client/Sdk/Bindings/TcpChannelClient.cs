using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk.Bindings
{
    public class TcpChannelClient : IChannelClient
    {
        readonly TcpClient _client;

        public TcpChannelClient(TcpClient client)
        {
            _client = client;
        }

        public bool Connected => _client.Connected;

        public int PreferedSendBufferSize { get => _client.SendBufferSize; set => _client.SendBufferSize = value; }
        public int PreferedReceiveBufferSize { get => _client.ReceiveBufferSize; set => _client.ReceiveBufferSize = value; }

        public void Dispose() => _client.Dispose();

        public Stream GetStream() => _client.GetStream();
    }
}
