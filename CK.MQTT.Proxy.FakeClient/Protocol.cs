using System;
using System.Collections.Generic;
using System.Text;

namespace CK.MQTT.Proxy.FakeClient
{
    enum StubClientHeader : byte
    {
        Disconnect,
        Connect,
        ConnectAnonymous,
        Publish,
        Subscribe,
        Unsubscribe,
        IsConnected,
        EndOfStream = byte.MaxValue
    }

    enum RelayHeader : byte
    {
        ConnectResponse,
        IsConnectedResponse,
        Disconnected,
        MessageEvent,
        EndOfStream = byte.MaxValue
    }
}
