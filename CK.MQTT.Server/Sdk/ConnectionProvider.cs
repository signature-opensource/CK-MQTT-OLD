using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CK.Core;
using CK.MQTT.Sdk.Packets;

namespace CK.MQTT.Sdk
{
    internal class ConnectionProvider : IConnectionProvider
    {
        static readonly IList<string> _privateClients;
        static readonly ConcurrentDictionary<string, IMqttChannel<IPacket>> _connections;
        static readonly object _lockObject = new object();

        static ConnectionProvider()
        {
            _privateClients = new List<string>();
            _connections = new ConcurrentDictionary<string, IMqttChannel<IPacket>>();
        }

        public int Connections => _connections.Count;

        public IEnumerable<string> ActiveClients
        {
            get
            {
                return _connections
                    .Where( c => c.Value.IsConnected )
                    .Select( c => c.Key );
            }
        }

        public IEnumerable<string> PrivateClients => _privateClients;

        public void RegisterPrivateClient( string clientId )
        {
            if( _privateClients.Contains( clientId ) )
            {
                var message = string.Format( ServerProperties.ConnectionProvider_PrivateClientAlreadyRegistered, clientId );

                throw new MqttServerException( message );
            }

            lock( _lockObject )
            {
                _privateClients.Add( clientId );
            }
        }

        public void AddConnection( IActivityMonitor m, string clientId, IMqttChannel<IPacket> connection )
        {
            if( _connections.TryGetValue( clientId, out _ ) )
            {
                m.Warn( string.Format( ServerProperties.ConnectionProvider_ClientIdExists, clientId ) );
                RemoveConnection( m, clientId );
            }

            _connections.TryAdd( clientId, connection );
        }

        public IMqttChannel<IPacket> GetConnection( IActivityMonitor m, string clientId )
        {

            if( _connections.TryGetValue( clientId, out IMqttChannel<IPacket> existingConnection ) )
            {
                if( !existingConnection.IsConnected )
                {
                    m.Warn( string.Format( ServerProperties.ConnectionProvider_ClientDisconnected, clientId ) );

                    RemoveConnection(m, clientId );
                    existingConnection = default;
                }
            }

            return existingConnection;
        }

        public void RemoveConnection( IActivityMonitor m, string clientId )
        {

            if( _connections.TryRemove( clientId, out IMqttChannel<IPacket> existingConnection ) )
            {
                m.Info( string.Format( ServerProperties.ConnectionProvider_RemovingClient, clientId ) );

                existingConnection.Dispose();
            }

            if( _privateClients.Contains( clientId ) )
            {
                lock( _lockObject )
                {
                    if( _privateClients.Contains( clientId ) )
                    {
                        _privateClients.Remove( clientId );
                    }
                }
            }
        }
    }
}
