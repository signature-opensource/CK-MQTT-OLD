using System;
using System.Collections.Generic;
using System.Text;

namespace CK.MQTT
{
    public static class MqttProtocol
    {
        /// <summary>
        /// Default port for using secure communication on MQTT, which is 8883
        /// </summary>
		public const int DefaultSecurePort = 8883;

        /// <summary>
        /// Default port for using non secure communication on MQTT, which is 1883
        /// </summary>
		public const int DefaultNonSecurePort = 1883;
    }
}
