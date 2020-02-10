namespace CK.MQTT.Proxy.FakeClient
{
    enum StubClientHeader : byte
    {
        Disconnect,
        Connect,
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
