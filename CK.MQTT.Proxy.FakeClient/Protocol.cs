using System;
using System.Collections.Generic;
using System.Text;

namespace CK.MQTT.Proxy.FakeClient
{
    enum ClientHeader : byte
    {
        Disconnect,
        Connect,
        Publish,
        Subscribe,
        Unsubscribe,
        IsConnected
    }

    enum ServerHeader
    {
        Disconnected,
        MessageEvent
    }
}
