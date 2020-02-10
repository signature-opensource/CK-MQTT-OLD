using CK.MQTT.Sdk.Packets;
using System.Collections.Generic;

namespace CK.MQTT.Sdk
{
    public interface IConnectionProvider
    {
        int Connections { get; }

        IEnumerable<string> ActiveClients { get; }

        IEnumerable<string> PrivateClients { get; }

        void RegisterPrivateClient( string clientId );

        void AddConnection( string clientId, IMqttChannel<IPacket> connection );

        IMqttChannel<IPacket> GetConnection( string clientId );

        void RemoveConnection( string clientId );
    }
}
