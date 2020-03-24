using CK.MQTT.Sdk.Packets;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Resources;

namespace CK.MQTT
{
    public static class ClientProperties
    {
        public static string ConnectFormatter_InvalidClientIdFormat( string id ) => $"{id} is an invalid ClientId. It must contain only numbers and letters";
        public static string ConnectFormatter_InvalidProtocolName( string protocolName ) => $"{protocolName} is not a valid protocol name";
        public static string SubscribeAckFormatter_MissingReturnCodes => "A subscribe acknowledge packet must contain at least one return code";
        public static string SubscribeFormatter_MissingTopicFilterQosPair => "A subscribe packet must contain at least one Topic Filter / QoS pair";
        public static string Client_ConnectionTimeout( string clientId ) => $"A timeout occured while waiting for the client {clientId} connection confirmation";
        public static string Client_SubscribeTimeout( string clientId, string topicName ) => $"A timeout occured while waiting for the subscribe confirmation of client {clientId} for topic {topicName}";
        public static string Client_UnsubscribeTimeout( string clientId, string topicName ) => $"A timeout occured while waiting for the unsubscribe confirmation of client {clientId} for topics: {topicName}";
        public static string ProtocolFlowProvider_UnknownPacketType( MqttPacketType packetType ) => $"An error occured while trying to get a Flow Type based on Packet Type {packetType}";
        public static string MqttChannel_DisposeError( SocketError errorCode ) => $"An error occurred while closing underlying communication channel. Error code: {errorCode}";
        public static string TcpChannelFactory_TcpClient_Failed( string address, int port ) => $"An error occurred while connecting via TCP to the endpoint address {address} and port {port}, to establish an MQTT connection";
        public static string Client_InitializeError => "An error occurred while initializing a client";
        public static string TcpChannelProvider_TcpListener_Failed => "An error occurred while starting to listen incoming TCP connections";
        public static string Client_ConnectionError( string clientId ) => $"An error occurred while trying to connect the client {clientId}";
        public static string Client_SubscribeError( string clientId, string topicName ) => $"An error occurred while trying to subscribe the client {clientId} to topic {topicName}";
        public static string Client_UnsubscribeError( string clientId, string topicName ) => $"An error occurred while trying to unsubscribe the client {clientId} of topics: {topicName}";
        public static string PacketChannelFactory_InnerChannelFactoryNotFound => "An inner channel factory is required to create a new packet channel";
        public static string UnsubscribeFormatter_MissingTopics => "An unsubscribe packet must contain at least one topic to unsubscribe";
        public static string Client_AnonymousClientWithoutCleanSession => "Anonymous clients must set the \"cleanSession\" parameter to true in order to perform the protocol connection";
        public static string ByteExtensions_InvalidBitPosition => "Bit position must be between 0 and 7";
        public static string ConnectAckFormatter_InvalidAckFlags => "Bits 7-1 from Acknowledge flags are reserved and must be set to 0";
        public static string ByteExtensions_InvalidByteIndex => "Byte index must be from 1 to 8, starting from msb";
        public static string ClientPacketListener_Error => "Client - An error occurred while listening and dispatching packets";
        public static string Client_PacketsObservableCompleted => "Client - Packet observable sequence has been completed, hence closing the channel";
        public static string Client_NewApplicationMessageReceived( string clientId, string topicName ) => $"Client {clientId} - An application message for topic {topicName} was received";
        public static string Client_CleanedOldSession( string clientId ) => $"Client {clientId} - Cleaned old session";
        public static string Client_CreatedSession( string clientId ) => $"Client {clientId} - Created new client session";
        public static string ClientPacketListener_DispatchingMessage( string clientId, MqttPacketType packetType, string flowTypeName ) => $"Client {clientId} - Dispatching {packetType} message to flow {flowTypeName}";
        public static string ClientPacketListener_DispatchingPublish( string clientId, string packetType, string flowTypeName ) => $"Client {clientId} - Dispatching Publish message to flow {packetType} and topic {flowTypeName}";
        public static string ClientPacketListener_FirstPacketReceived( string clientId, MqttPacketType packetType ) => $"Client {clientId} - First packet from Server has been received. Type: {packetType}";
        public static string ClientPacketListener_PacketChannelCompleted( string clientId ) => $"Client {clientId} - Packet Channel observable sequence has been completed";
        public static string Client_DeletedSessionOnDisconnect( string clientId ) => $"Client {clientId} - Removed client session";
        public static string ConnectFormatter_ClientIdMaxLengthExceeded => "Client Id cannot exceed 23 bytes";
        public static string ConnectFormatter_ClientIdEmptyRequiresCleanSession => "Client Ids with zero bytes length requires the Clean Session value to be 1 (true)";
        public static string Mqtt_Disposing( string name ) => $"Disposing {name}...";
        public static string PublishFormatter_InvalidDuplicatedWithQoSZero => "Duplicated flag must be set to 0 if the QoS is 0";
        public static string Formatter_InvalidHeaderFlag( byte headerFlag, string packetName, int expectedFlag ) => $"Header Flag {headerFlag} is invalid for {packetName} packet. Expected value: {expectedFlag}";
        public static string SessionRepository_ClientSessionNotFound( string clientId ) => $"No session has been found for client {clientId}";
        public static string PublishFormatter_InvalidPacketId => "Packet Id is not allowed for packets with QoS 0";
        public static string PublishFormatter_PacketIdRequired => "Packet Id value cannot be null or empty for packets with QoS 1 or 2";
        public static string PublishReceiverFlow_PacketIdNotAllowed => "Packet Id value is not allowed for QoS 0";
        public static string PublishReceiverFlow_PacketIdRequired => "Packet Id value is required for QoS major than 0";
        public static string ConnectFormatter_InvalidPasswordFlag => "Password Flag must be set to 0 if the User Name Flag is set to 0";
        public static string ConnectFormatter_PasswordNotAllowed => "Password value must be null or empty if User value is null or empty";
        public static string ConnectFormatter_UnsupportedLevel( byte protocolLevel ) => $"Protocol Level {protocolLevel} is not supported by the server";
        public static string Formatter_InvalidQualityOfService => "Qos value must be from 0x00 to 0x02";
        public static string MqttChannel_ReceivedPacket( int byteAmount ) => $"Received packet of {byteAmount} bytes";
        public static string ConnectFormatter_InvalidReservedFlag => "Reserved Flag must be always set to 0";
        public static string SubscribeAckFormatter_InvalidReturnCodes => "Return codes can only be valid QoS values or a failure code (0x80)";
        public static string MqttChannel_SendingPacket( int length ) => $"Sending packet of {length} bytes";
        public static string ConnectAckFormatter_InvalidSessionPresentForErrorReturnCode => "Session Present flag must be set to 0 for non-zero return codes";
        public static string PublishFlow_RetryingQoSFlow( MqttPacketType type, string clientId ) => $"The ack for message {type} has not been received. Re sending message for client {clientId}";
        public static string Client_ConnectNotAccepted( string clientId, MqttConnectionStatus status ) => $"The connect packet of client {clientId} has not been accepted by the server. Status: {status}. The connection will be closed";
        public static string ClientPacketListener_FirstReceivedPacketMustBeConnectAck => "The first packet received from the Server must be a ConnectAck packet. The connection will be closed.";
        public static string Formatter_InvalidPacket( string typeName ) => $"The packet sent cannot be handled by {typeName}";
        public static string ProtocolFlowProvider_InvalidPacketType( MqttPacketType packetType ) => $"The packet type {packetType} cannot be handled by this flow provider";
        public static string Client_AlreadyDisconnected => "The protocol disconnection cannot be performed because the client is already disconnected";
        public static string PacketManager_PacketUnknown => "The received packet cannot be handled by any of the registered formatters";
        public static string TopicEvaluator_InvalidTopicFilter( string topicFilter ) => $"The topic filter {topicFilter} is invalid according to the protocol rules and configuration";
        public static string TopicEvaluator_InvalidTopicName( string topicName ) => $"The topic name {topicName} is invalid according to the protocol rules";
        public static string MqttChannel_NetworkStreamCompleted => "The underlying communication stream has completed sending bytes. The observable sequence will be completed and the channel will be disposed";
        public static string MqttChannel_StreamDisconnected => "The underlying communication stream is not available. The socket could have been disconnected";
        public static string MqttChannel_ClientNotConnected => "The underlying communication stream is not connected";
        public static string Client_UnexpectedChannelDisconnection => "The underlying connection has been disconnected unexpectedly";
        public static string SubscribeFormatter_InvalidTopicFilter( string topicFilter ) => $"Topic filter {topicFilter} is invalid. See protocol specification for more details on Topic Filter rules: http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/os/mqtt-v3.1.1-os.html#_Toc398718106";
        public static string PublishFormatter_InvalidTopicName( string topicName ) => $"Topic name {topicName} is invalid. It cannot be null or empty and It must not contain wildcard characters";
        public static string ConnectFormatter_InvalidWillRetainFlag => "Will Retain Flag must be set to 0 if the Will Flag is set to 0";
    }
}
