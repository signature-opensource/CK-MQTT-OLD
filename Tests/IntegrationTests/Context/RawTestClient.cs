using CK.MQTT;
using IntegrationTests.Context;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationTests.Context
{
    public class TcpRawTestClient : IRawTestClient
    {
        readonly TcpClient _tcpClient;
        Stream _stream;


        public Stream Stream { get => _stream; set => _stream = value; }

        public TcpRawTestClient( MqttConfiguration conf )
        {
            _tcpClient = new TcpClient( "127.0.0.1", conf.Port );
            _stream = _tcpClient.GetStream();
        }



        public ValueTask SendRawBytesAsync( ReadOnlyMemory<byte> bytes )
        {
            return _stream.WriteAsync( bytes );
        }

        public void Dispose()
        {
            _tcpClient.Dispose();
            _stream.Dispose();
        }
    }
}
