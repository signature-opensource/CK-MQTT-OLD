using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Resources;

namespace CK.MQTT
{
    public static class ServerProperties
    {
        public const string ConnectionProvider_ClientDisconnected = "Server - The connection for client {0} is not connected. Removing connection";
        public const string ConnectionProvider_ClientIdExists = "An active connection already exists for client {0}. Disposing current connection and adding the new one";
        public const string ConnectionProvider_PrivateClientAlreadyRegistered = "A private client with Id {0} is already registered";
        public const string ConnectionProvider_RemovingClient = "Server - Removing connection of client {0}";
        public const string DisconnectFlow_Disconnecting = "Server - Disconnecting client {0}";
        public const string Mqtt_Disposing = "Disposing {0}...";
        public const string PacketChannelCompleted = "Server - Packet Channel observable sequence has been completed for client {0}";
        public const string Server_CleanedOldSession = "Server - Cleaned old session for client {0}";
        public const string Server_CreatedSession = "Server - Created new session for client {0}";
        public const string Server_DeletedSessionOnDisconnect = "Server - Removed session for client {0} as part of Disconnect flow";
        public const string Server_InitializeError = "An error occurred while initializing the server";
        public const string Server_NewSocketAccepted = "Server - A new TCP channel has been accepted";
        public const string Server_NotStartedError = "The Server has to be started first, in order to execute any operation";
        public const string Server_PacketsObservableCompleted = "Server - Packet observable sequence has been completed, hence closing the channel";
        public const string Server_PacketsObservableError = "Server - Packet observable sequence had an error, hence closing the channel";
        public const string ServerPacketListener_ConnectionError = "Server - An error occurred while executing the connect flow. Client: {0}";
        public const string ServerPacketListener_ConnectPacketReceived = "Server - A connect packet has been received from client {0}";
        public const string ServerPacketListener_DispatchingMessage = "Server - Dispatching {0} message to flow {1} for client {2}";
        public const string ServerPacketListener_DispatchingPublish = "Server - Dispatching Publish message to flow {0} for client {1} and topic {2}";
        public const string ServerPacketListener_DispatchingSubscribe = "Server - Dispatching Subscribe message to flow {0} for client {1} and topics: {2}";
        public const string ServerPacketListener_Error = "Server - An error occurred while listening and dispatching packets - Client: {0}";
        public const string ServerPacketListener_FirstPacketMustBeConnect = "The first packet sent by a Client must be a Connect. The connection will be closed.";
        public const string ServerPacketListener_KeepAliveTimeExceeded = "The keep alive tolerance of {0} seconds has been exceeded and no packet has been received from client {1}. The connection will be closed.";
        public const string ServerPacketListener_NoConnectReceived = "No connect packet has been received since the network connection was established. The connection will be closed.";
        public const string ServerPacketListener_SecondConnectNotAllowed = "Only one Connect packet is allowed. The connection will be closed.";
        public const string ServerPublishReceiverFlow_SendingWill = "Server - Sending last will message of client {0} to topic {1}";
        public const string ServerPublishReceiverFlow_SystemMessageNotAllowedForClient = "Publish messages with a leading $ in the topic are considered  server specific messages, hence remote clients are not allowed to publish them";
        public const string ServerPublishReceiverFlow_TopicNotSubscribed = "The topic {0} has no subscribers, hence the message sent by {1} will not be forwarded";
        public const string ServerSubscribeFlow_ErrorOnSubscription = "Server - An error occurred when subscribing client {0} to topic {1}";
        public const string ServerSubscribeFlow_InvalidTopicSubscription = "Server - The topic {0}, sent by client {1} is invalid. Returning failure code";
        public const string SessionRepository_ClientSessionNotFound = "No session has been found for client {0}";
        public const string TcpChannelProvider_TcpListener_Failed = "An error occurred while starting to listen incoming TCP connections";
    }
}
