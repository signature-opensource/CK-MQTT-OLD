using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CK.MQTT
{
    /// <summary>
    /// A MQTT Client that guarantee that the messages will be sent to the server.
    /// If the connection drop, an implementation may store the message until the connection is brought back.
    /// </summary>
    interface ISimpleMqttClient : IMqttClientBase
    {
    }
}
