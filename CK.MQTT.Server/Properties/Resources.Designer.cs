﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace CK.MQTT.Server.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("CK.MQTT.Server.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Server - The connection for client {0} is not connected. Removing connection.
        /// </summary>
        internal static string ConnectionProvider_ClientDisconnected {
            get {
                return ResourceManager.GetString("ConnectionProvider_ClientDisconnected", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An active connection already exists for client {0}. Disposing current connection and adding the new one.
        /// </summary>
        internal static string ConnectionProvider_ClientIdExists {
            get {
                return ResourceManager.GetString("ConnectionProvider_ClientIdExists", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to A private client with Id {0} is already registered.
        /// </summary>
        internal static string ConnectionProvider_PrivateClientAlreadyRegistered {
            get {
                return ResourceManager.GetString("ConnectionProvider_PrivateClientAlreadyRegistered", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Server - Removing connection of client {0}.
        /// </summary>
        internal static string ConnectionProvider_RemovingClient {
            get {
                return ResourceManager.GetString("ConnectionProvider_RemovingClient", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Server - Disconnecting client {0}.
        /// </summary>
        internal static string DisconnectFlow_Disconnecting {
            get {
                return ResourceManager.GetString("DisconnectFlow_Disconnecting", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Disposing {0}....
        /// </summary>
        internal static string Mqtt_Disposing {
            get {
                return ResourceManager.GetString("Mqtt_Disposing", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Server - Packet Channel observable sequence has been completed for client {0}.
        /// </summary>
        internal static string PacketChannelCompleted {
            get {
                return ResourceManager.GetString("PacketChannelCompleted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Server - Cleaned old session for client {0}.
        /// </summary>
        internal static string Server_CleanedOldSession {
            get {
                return ResourceManager.GetString("Server_CleanedOldSession", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Server - Created new session for client {0}.
        /// </summary>
        internal static string Server_CreatedSession {
            get {
                return ResourceManager.GetString("Server_CreatedSession", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Server - Removed session for client {0} as part of Disconnect flow.
        /// </summary>
        internal static string Server_DeletedSessionOnDisconnect {
            get {
                return ResourceManager.GetString("Server_DeletedSessionOnDisconnect", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An error occurred while initializing the server.
        /// </summary>
        internal static string Server_InitializeError {
            get {
                return ResourceManager.GetString("Server_InitializeError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Server - A new TCP channel has been accepted.
        /// </summary>
        internal static string Server_NewSocketAccepted {
            get {
                return ResourceManager.GetString("Server_NewSocketAccepted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The Server has to be started first, in order to execute any operation.
        /// </summary>
        internal static string Server_NotStartedError {
            get {
                return ResourceManager.GetString("Server_NotStartedError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Server - Packet observable sequence has been completed, hence closing the channel.
        /// </summary>
        internal static string Server_PacketsObservableCompleted {
            get {
                return ResourceManager.GetString("Server_PacketsObservableCompleted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Server - Packet observable sequence had an error, hence closing the channel.
        /// </summary>
        internal static string Server_PacketsObservableError {
            get {
                return ResourceManager.GetString("Server_PacketsObservableError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Server - An error occurred while executing the connect flow. Client: {0}.
        /// </summary>
        internal static string ServerPacketListener_ConnectionError {
            get {
                return ResourceManager.GetString("ServerPacketListener_ConnectionError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Server - A connect packet has been received from client {0}.
        /// </summary>
        internal static string ServerPacketListener_ConnectPacketReceived {
            get {
                return ResourceManager.GetString("ServerPacketListener_ConnectPacketReceived", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Server - Dispatching {0} message to flow {1} for client {2}.
        /// </summary>
        internal static string ServerPacketListener_DispatchingMessage {
            get {
                return ResourceManager.GetString("ServerPacketListener_DispatchingMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Server - Dispatching Publish message to flow {0} for client {1} and topic {2}.
        /// </summary>
        internal static string ServerPacketListener_DispatchingPublish {
            get {
                return ResourceManager.GetString("ServerPacketListener_DispatchingPublish", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Server - Dispatching Subscribe message to flow {0} for client {1} and topics: {2}.
        /// </summary>
        internal static string ServerPacketListener_DispatchingSubscribe {
            get {
                return ResourceManager.GetString("ServerPacketListener_DispatchingSubscribe", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Server - An error occurred while listening and dispatching packets - Client: {0}.
        /// </summary>
        internal static string ServerPacketListener_Error {
            get {
                return ResourceManager.GetString("ServerPacketListener_Error", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The first packet sent by a Client must be a Connect. The connection will be closed..
        /// </summary>
        internal static string ServerPacketListener_FirstPacketMustBeConnect {
            get {
                return ResourceManager.GetString("ServerPacketListener_FirstPacketMustBeConnect", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The keep alive tolerance of {0} seconds has been exceeded and no packet has been received from client {1}. The connection will be closed..
        /// </summary>
        internal static string ServerPacketListener_KeepAliveTimeExceeded {
            get {
                return ResourceManager.GetString("ServerPacketListener_KeepAliveTimeExceeded", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No connect packet has been received since the network connection was established. The connection will be closed..
        /// </summary>
        internal static string ServerPacketListener_NoConnectReceived {
            get {
                return ResourceManager.GetString("ServerPacketListener_NoConnectReceived", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Only one Connect packet is allowed. The connection will be closed..
        /// </summary>
        internal static string ServerPacketListener_SecondConnectNotAllowed {
            get {
                return ResourceManager.GetString("ServerPacketListener_SecondConnectNotAllowed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Server - Sending last will message of client {0} to topic {1}.
        /// </summary>
        internal static string ServerPublishReceiverFlow_SendingWill {
            get {
                return ResourceManager.GetString("ServerPublishReceiverFlow_SendingWill", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Publish messages with a leading $ in the topic are considered  server specific messages, hence remote clients are not allowed to publish them.
        /// </summary>
        internal static string ServerPublishReceiverFlow_SystemMessageNotAllowedForClient {
            get {
                return ResourceManager.GetString("ServerPublishReceiverFlow_SystemMessageNotAllowedForClient", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The topic {0} has no subscribers, hence the message sent by {1} will not be forwarded.
        /// </summary>
        internal static string ServerPublishReceiverFlow_TopicNotSubscribed {
            get {
                return ResourceManager.GetString("ServerPublishReceiverFlow_TopicNotSubscribed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Server - An error occurred when subscribing client {0} to topic {1}.
        /// </summary>
        internal static string ServerSubscribeFlow_ErrorOnSubscription {
            get {
                return ResourceManager.GetString("ServerSubscribeFlow_ErrorOnSubscription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Server - The topic {0}, sent by client {1} is invalid. Returning failure code.
        /// </summary>
        internal static string ServerSubscribeFlow_InvalidTopicSubscription {
            get {
                return ResourceManager.GetString("ServerSubscribeFlow_InvalidTopicSubscription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No session has been found for client {0}.
        /// </summary>
        internal static string SessionRepository_ClientSessionNotFound {
            get {
                return ResourceManager.GetString("SessionRepository_ClientSessionNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An error occurred while starting to listen incoming TCP connections.
        /// </summary>
        internal static string TcpChannelProvider_TcpListener_Failed {
            get {
                return ResourceManager.GetString("TcpChannelProvider_TcpListener_Failed", resourceCulture);
            }
        }
    }
}