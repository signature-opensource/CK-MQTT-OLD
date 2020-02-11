using CK.MQTT.Sdk.Packets;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CK.MQTT.Sdk
{
    internal class ConnectionProvider : IConnectionProvider
    {
        static readonly ITracer _tracer = Tracer.Get<ConnectionProvider>();
        static readonly IList<string> _privateClients = new List<string>();
        static readonly ConcurrentDictionary<string, IMqttChannel<IPacket>> _connections = new ConcurrentDictionary<string, IMqttChannel<IPacket>>();
        static readonly object _lockObject = new object();

        public int Connections => _connections.Count;

        public IEnumerable<string> ActiveClients =>
            _connections
                .Where( c => c.Value.IsConnected )
                .Select( c => c.Key );

        public IEnumerable<string> PrivateClients => _privateClients;

        public void RegisterPrivateClient( string clientId )
        {
            if( _privateClients.Contains( clientId ) )
            {
                throw new MqttServerException( ServerProperties.ConnectionProvider_PrivateClientAlreadyRegistered( clientId ) );
            }

            lock( _lockObject )
            {
                _privateClients.Add( clientId );
            }
        }

        public void AddConnection( string clientId, IMqttChannel<IPacket> connection )
        {
            if( _connections.TryGetValue( clientId, out _ ) )
            {
                _tracer.Warn( ServerProperties.ConnectionProvider_ClientIdExists( clientId ) );

                RemoveConnection( clientId );
            }

            _connections.TryAdd( clientId, connection );
        }

        public IMqttChannel<IPacket> GetConnection( string clientId )
        {
            if( _connections.TryGetValue( clientId, out IMqttChannel<IPacket> existingConnection ) )
            {
                if( !existingConnection.IsConnected )
                {
                    _tracer.Warn( ServerProperties.ConnectionProvider_ClientDisconnected( clientId ) );

                    RemoveConnection( clientId );
                    existingConnection = default;
                }
            }

            return existingConnection;
        }

        public void RemoveConnection( string clientId )
        {
            if( _connections.TryRemove( clientId, out IMqttChannel<IPacket> existingConnection ) )
            {
                _tracer.Info( ServerProperties.ConnectionProvider_RemovingClient( clientId ) );

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
