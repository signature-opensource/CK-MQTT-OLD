using CK.MQTT;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Text;

namespace Tests
{
    internal class MqttLastWillConverter : JsonConverter
    {
        public override bool CanWrite => false;

        public override bool CanConvert( Type objectType ) => objectType == typeof( MqttLastWill );

        public override object ReadJson( JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer )
        {
            JObject jObject = JObject.Load( reader );

            string topic = jObject["topic"].ToObject<string>();
            MqttQualityOfService qos = jObject["qualityOfService"].ToObject<MqttQualityOfService>();
            bool retain = jObject["retain"].ToObject<bool>();
            string message = jObject["message"].ToObject<string>();
            bool hasMessage = !string.IsNullOrEmpty( (message) );
            byte[] payload = hasMessage ? Encoding.UTF8.GetBytes( message ) : jObject["message"].ToObject<byte[]>();

            return new MqttLastWill( topic, qos, retain, payload );
        }

        public override void WriteJson( JsonWriter writer, object value, JsonSerializer serializer )
        {
            throw new NotImplementedException();
        }
    }
}
