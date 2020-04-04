using System;

namespace CK.MQTT.Common.Packets
{
    internal class Subscription : IEquatable<Subscription>
    {
        public Subscription( string topicFilter, MqttQualityOfService requestedQos )
        {
            TopicFilter = topicFilter;
            MaximumQualityOfService = requestedQos;
        }

        public string TopicFilter { get; set; }

        public MqttQualityOfService MaximumQualityOfService { get; set; }

        public bool Equals( Subscription other )
        {
            if( other == null ) return false;

            return TopicFilter == other.TopicFilter &&
                MaximumQualityOfService == other.MaximumQualityOfService;
        }

        public override bool Equals( object obj )
        {
            if( obj == null ) return false;
            if( !(obj is Subscription subscription) ) return false;

            return Equals( subscription );
        }

        public static bool operator ==( Subscription subscription, Subscription other )
        {
            if( subscription is null || other is null ) return Equals( subscription, other );
            return subscription.Equals( other );
        }

        public static bool operator !=( Subscription subscription, Subscription other )
        {
            if( subscription is null || other is null ) return !Equals( subscription, other );
            return !subscription.Equals( other );
        }

        public override int GetHashCode() => TopicFilter.GetHashCode() + MaximumQualityOfService.GetHashCode();
    }
}
