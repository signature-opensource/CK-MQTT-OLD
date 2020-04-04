using System;
using System.Collections.Generic;
using System.Text;

namespace CK.MQTT
{
    public readonly struct ConnectResult
    {
        public ConnectResult(SessionState sessionState, ConnectionStatus connectionStatus)
        {
            SessionState = sessionState;
            ConnectionStatus = connectionStatus;
        }

        public readonly SessionState SessionState;

        public readonly ConnectionStatus ConnectionStatus;
    }
}
