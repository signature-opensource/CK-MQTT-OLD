using CK.MQTT.Sdk.Packets;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Resources;

namespace CK.MQTT
{
    public static class ServerProperties
    {
        public static string ConnectionProvider_ClientDisconnected( string clientId ) => $"Server - The connection for client {clientId} is not connected. Removing connection";
        public static string ConnectionProvider_ClientIdExists( string clientId ) => $"An active connection already exists for client {clientId}. Disposing current connection and adding the new one";
        public static string ConnectionProvider_PrivateClientAlreadyRegistered( string clientId ) => $"A private client with Id {clientId} is already registered";
        public static string ConnectionProvider_RemovingClient( string clientId ) => $"Server - Removing connection of client {clientId}";
        public static string DisconnectFlow_Disconnecting( string clientId ) => $"Server - Disconnecting client {clientId}";
        public static string PacketChannelCompleted( string clientId ) => $"Server - Packet Channel observable sequence has been completed for client {clientId}";
        public static string Server_CleanedOldSession( string clientId ) => $"Server - Cleaned old session for client {clientId}";
        public static string Server_CreatedSession( string clientId ) => $"Server - Created new session for client {clientId}";
        public static string Server_DeletedSessionOnDisconnect( string clientId ) => $"Server - Removed session for client {clientId} as part of Disconnect flow";
        public static string Server_InitializeError => "An error occurred while initializing the server";
        public static string Server_NewSocketAccepted => "Server - A new TCP channel has been accepted";
        public static string Server_NotStartedError => "The Server has to be started first, in order to execute any operation";
        public static string Server_PacketsObservableError => "Server - Packet observable sequence had an error, hence closing the channel";
        public static string ServerPacketListener_ConnectionError( string clientId ) => $"Server - An error occurred while executing the connect flow. Client: {clientId}";
        public static string ServerPacketListener_ConnectPacketReceived( string clientId ) => $"Server - A connect packet has been received from client {clientId}";
        public static string ServerPacketListener_DispatchingMessage( MqttPacketType packetType, string flowName, string clientId ) => $"Server - Dispatching {packetType} message to flow {flowName} for client {clientId}";
        public static string ServerPacketListener_DispatchingPublish( string flowName, string clientId, string topicName ) => $"Server - Dispatching Publish message to flow {flowName} for client {clientId} and topic {topicName}";
        public static string ServerPacketListener_DispatchingSubscribe( string flowName, string clientId, string topics ) => $"Server - Dispatching Subscribe message to flow {flowName} for client {clientId} and topics: {topics}";
        public static string ServerPacketListener_Error( string clientId ) => $"Server - An error occurred while listening and dispatching packets - Client: {clientId}";
        public static string ServerPacketListener_FirstPacketMustBeConnect => "The first packet sent by a Client must be a Connect. The connection will be closed.";
        public static string ServerPacketListener_KeepAliveTimeExceeded( TimeSpan tolerance, string clientId ) => $"The keep alive tolerance of {tolerance} seconds has been exceeded and no packet has been received from client {clientId}. The connection will be closed.";
        public static string ServerPacketListener_NoConnectReceived => "No connect packet has been received since the network connection was established. The connection will be closed.";
        public static string ServerPacketListener_SecondConnectNotAllowed => "Only one Connect packet is allowed. The connection will be closed.";
        public static string ServerPublishReceiverFlow_SystemMessageNotAllowedForClient => "Publish messages with a leading $ in the topic are considered  server specific messages, hence remote clients are not allowed to publish them";
        public static string ServerPublishReceiverFlow_TopicNotSubscribed( string topicName, string clientId ) => $"The topic {topicName} has no subscribers, hence the message sent by {clientId} will not be forwarded";
        public static string ServerSubscribeFlow_ErrorOnSubscription( string clientId, string topicName ) => $"Server - An error occurred when subscribing client {clientId} to topic {topicName}";
        public static string ServerSubscribeFlow_InvalidTopicSubscription( string topicName, string clientId ) => $"Server - The topic {topicName}, sent by client {clientId} is invalid. Returning failure code";
        public static string SessionRepository_ClientSessionNotFound( string clientId ) => $"No session has been found for client {clientId}";
        public static string TcpChannelProvider_TcpListener_Failed => "An error occurred while starting to listen incoming TCP connections";

    }
}
