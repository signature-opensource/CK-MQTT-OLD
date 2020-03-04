using CK.Core;
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

        void AddConnection( IActivityMonitor m, string clientId, IMqttChannel<IPacket> connection );

        IMqttChannel<IPacket> GetConnection( IActivityMonitor m, string clientId );

        void RemoveConnection( IActivityMonitor m, string clientId );
    }
}
