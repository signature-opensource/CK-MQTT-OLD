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
    public interface IRawTestClient : IDisposable
    {
        Stream Stream { get; set; }
        ValueTask SendRawBytesAsync( ReadOnlyMemory<byte> bytes );
    }
}
