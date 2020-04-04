namespace CK.MQTT.Sdk.Flows
{
    internal enum ProtocolFlowType
    {
        Connect,
        PublishSender,
        PublishReceiver,
        Subscribe,
        Unsubscribe,
        Ping,
        Disconnect
    }
}
