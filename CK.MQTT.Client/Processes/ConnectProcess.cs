using CK.Core;
using CK.MQTT.Sdk;
using CK.MQTT.Sdk.Packets;
using System;
using System.Net;
using System.Threading.Tasks;

namespace CK.MQTT.Client.Processes
{
    static class ConnectProcess
    {
        public static async Task<ConnectResult> ExecuteConnectProtocol(
            IActivityMonitor m, IMqttChannel<IPacket> channel,
            string clientId, bool cleanSession, byte protocolLevel,
            string userName, string password, MqttLastWill? will,
            ushort keepAliveSecs, int waitTimeoutSecs )
        {
            using( m.OpenInfo( "Executing connect protocol..." ) )
            {
                Connect connect = new Connect( clientId, cleanSession, protocolLevel )
                {
                    UserName = userName,
                    Password = password,
                    Will = will,
                    KeepAlive = keepAliveSecs
                };
                IPacket? ack = await channel.SendAndWaitResponseWithAdditionalLogging( m, connect, null, receiveTimeoutMillisecond: waitTimeoutSecs * 1000 );
                if( ack == null )
                {
                    throw new TimeoutException( $"While connecting, the server did not replied a CONNACK packet in {waitTimeoutSecs} secs." );
                }
                if( !(ack is ConnectAck connectAck) )
                {
                    throw new ProtocolViolationException( "While connecting, the server replied with a packet that was not a CONNACK." );
                }
                return new ConnectResult( connectAck.SessionState, connectAck.Status );
            }
        }
    }
}
