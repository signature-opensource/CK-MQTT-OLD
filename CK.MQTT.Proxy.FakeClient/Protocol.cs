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
        IsConnected
    }

    enum RelayHeader
    {
        Disconnected,
        MessageEvent
    }
}
