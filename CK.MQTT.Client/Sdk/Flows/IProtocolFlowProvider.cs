using CK.MQTT.Sdk.Packets;

namespace CK.MQTT.Sdk.Flows
{
	internal interface IProtocolFlowProvider
	{
		IProtocolFlow GetFlow (MqttPacketType packetType);

		T GetFlow<T> () where T : class, IProtocolFlow;
	}
}
