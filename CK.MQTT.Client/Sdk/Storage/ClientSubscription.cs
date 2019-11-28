namespace CK.MQTT.Sdk.Storage
{
	internal class ClientSubscription
	{
		public string ClientId { get; set; }

		public string TopicFilter { get; set; }

		public MqttQualityOfService MaximumQualityOfService { get; set; }
	}
}
