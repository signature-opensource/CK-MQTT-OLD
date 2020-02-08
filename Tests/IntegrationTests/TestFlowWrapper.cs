using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CK.MQTT.Sdk;
using CK.MQTT.Sdk.Flows;
using CK.MQTT.Sdk.Packets;

namespace IntegrationTests
{
    internal class TestFlowWrapper : IProtocolFlow
    {
        readonly IProtocolFlow _flow;
        private readonly Action<(string clientId, IPacket input, IMqttChannel<IPacket> channel>> _callback;

        public TestFlowWrapper( IProtocolFlow flow, Action<(string clientId, IPacket input, IMqttChannel<IPacket> channel>> callback )
        {
            _flow = flow;
            _callback = callback;
        }

        public Task ExecuteAsync( string clientId, IPacket input, IMqttChannel<IPacket> channel )
        {
            _callback( (clientId, input, channel) );
            return _flow.ExecuteAsync( clientId, input, channel );
        }
    }
}
