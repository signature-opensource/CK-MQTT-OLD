using CK.Core;
using CK.MQTT.Sdk;
using CK.MQTT.Sdk.Bindings;
using System;
using System.Threading.Tasks;

namespace CK.MQTT
{
    /// <summary>
    /// Creates instances of <see cref="IMqttClient"/> for connecting to 
    /// an MQTT server.
    /// </summary>
    public static class MqttClientOld
    {
        /// <summary>
        /// Creates an <see cref="IMqttClient"/> and connects it to the destination 
        /// <paramref name="hostAddress"/> server using the specified transport binding 
        /// and MQTT configuration to customize the protocol parameters.
        /// </summary>
        /// <param name="hostAddress">
        /// Host address to use for the connection
        /// </param>
        /// <param name="configuration">
        /// The configuration used for creating the Client.
        /// See <see cref="MqttConfiguration" /> for more details about the supported values.
        /// </param>
        /// <param name="binding">
        /// The binding to use as the underlying transport layer.
        /// Deafault value: <see cref="TcpBinding"/>
        /// See <see cref="IMqttBinding"/> for more details about how 
        /// to implement a custom binding
        /// </param>
        /// <returns>A new MQTT Client</returns>
        public static Task<IMqttClient> CreateAsync( IActivityMonitor m, string hostAddress, MqttConfiguration configuration, IMqttBinding binding = null ) =>
            new MqttClientFactory( hostAddress, binding ?? new TcpBinding() ).CreateClientAsync( m, configuration );

        /// <summary>
        /// Creates an <see cref="IMqttClient"/> and connects it to the destination 
        /// <paramref name="hostAddress"/> server via TCP using the specified port.
        /// </summary>
        /// <returns>A new MQTT Client</returns>
        public static Task<IMqttClient> CreateAsync( IActivityMonitor m, string hostAddress, int port ) =>
            new MqttClientFactory( hostAddress ).CreateClientAsync( m, new MqttConfiguration { Port = port } );

        /// <summary>
        /// Creates an <see cref="IMqttClient"/> and connects it to the destination 
        /// <paramref name="hostAddress"/> server via TCP using the protocol defaults.
        /// </summary>
        /// <returns>A new MQTT Client</returns>
        public static Task<IMqttClient> CreateAsync( IActivityMonitor m, string hostAddress ) =>
            new MqttClientFactory( hostAddress ).CreateClientAsync( m, new MqttConfiguration() );

        internal static string GetPrivateClientId() =>
                "private" + Guid.NewGuid().ToString().Replace( "-", "" ).Substring( 0, 10 );

        
    }
}
