using CK.MQTT.Packets;
using CK.MQTT.Sdk.Packets;
using System;
using System.Collections.Generic;
using System.Text;

namespace CK.MQTT.Tests.Files.Packets
{
    static class Packets
    {
        public static readonly Publish Publish_Full = new Publish( "foo/bar", Encoding.UTF8.GetBytes( "this is the payload of this test packet" ), MqttQualityOfService.AtLeastOnce, true, true, 10 );
        public static readonly Publish Publish_Min = new Publish( "empty/packets", new ReadOnlyMemory<byte>(), MqttQualityOfService.AtMostOnce, false, false );
        public static readonly Publish Publish_Invalid_PacketId = new Publish( "", Encoding.UTF8.GetBytes( "this is the payload of this test packet" ), MqttQualityOfService.AtMostOnce, true, false, 10 );
        public static readonly Publish Publish_Invalid_Topic = new Publish( "", Encoding.UTF8.GetBytes( "this is the payload of this test packet" ), MqttQualityOfService.AtMostOnce, true, false );
        public static readonly Publish Publish_Invalid_Duplicated = new Publish( "foo/bar", Encoding.UTF8.GetBytes( "this is the payload of this test packet" ), MqttQualityOfService.AtMostOnce, true, true );
        public static readonly UnsubscribeAck UnsubscribeAck = new UnsubscribeAck( 10 );
        public static readonly Unsubscribe Unsubscribe_SingleTopic = new Unsubscribe( 10, new string[] { "foo/bar" } );
        public static readonly Unsubscribe Unsubscribe_MultiTopic = new Unsubscribe( 10, new string[] { "foo/bar", "foo/test", "a/b", "test/foo" } );
        public static readonly SubscribeAck SubscribeAck_SingleTopic = new SubscribeAck( 10, new SubscribeReturnCode[] { SubscribeReturnCode.MaximumQoS2 } );
        public static readonly SubscribeAck SubscribeAck_MultiTopic = new SubscribeAck( 10, new SubscribeReturnCode[] { SubscribeReturnCode.MaximumQoS2, SubscribeReturnCode.MaximumQoS1, SubscribeReturnCode.MaximumQoS0, SubscribeReturnCode.MaximumQoS1 } );
        public static readonly Subscribe Subscribe_MultiTopic = new Subscribe( 10, new Subscription[] { new Subscription( "foo/bar", MqttQualityOfService.ExactlyOnce ), new Subscription( "foo/test", MqttQualityOfService.AtLeastOnce ), new Subscription( "a/b", MqttQualityOfService.AtMostOnce ), new Subscription( "test/foo", MqttQualityOfService.AtLeastOnce ) } );
        public static readonly Subscribe Subscribe_SingleTopic = new Subscribe( 10, new Subscription[] { new Subscription( "foo/bar", MqttQualityOfService.ExactlyOnce ) } );
        public static readonly PublishRelease PublishRelease = new PublishRelease( 10 );
        public static readonly PublishReceived PublishReceived = new PublishReceived( 10 );
        public static readonly PublishComplete PublishComplete = new PublishComplete( 10 );
        public static readonly PublishAck PublishAck = new PublishAck( 10 );
        public static readonly ConnectAck ConnectAck = new ConnectAck( MqttConnectionStatus.BadUserNameOrPassword, false );
        public static readonly Connect Connect_Min = new Connect( "BarClient", true, 10 );
        public static readonly Connect Connect_Full = new Connect( "FooClient", true, 10 ) { Will = new MqttLastWill( "problems/disconnect", MqttQualityOfService.AtLeastOnce, true, Encoding.UTF8.GetBytes( "foo error" ) ), UserName = "foo", Password = "1234" };
    }
}
