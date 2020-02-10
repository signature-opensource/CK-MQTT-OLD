using CK.MQTT;
using CK.MQTT.Sdk;
using NUnit.Framework;

namespace Tests
{
    //Topic names based on Protocol Spec samples
    //http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/os/mqtt-v3.1.1-os.html#_Toc398718106
    public class TopicEvaluatorSpec
    {
        [Theory]
        [TestCase( "foo/bar" )]
        [TestCase( "foo/bar/" )]
        [TestCase( "/foo/bar" )]
        [TestCase( "/" )]
        public void when_evaluating_valid_topic_name_then_is_valid( string topicName )
        {
            MqttTopicEvaluator topicEvaluator = new MqttTopicEvaluator( new MqttConfiguration() );

            Assert.True( topicEvaluator.IsValidTopicName( topicName ) );
        }

        [Theory]
        [TestCase( "foo/+" )]
        [TestCase( "#" )]
        [TestCase( "" )]
        public void when_evaluating_invalid_topic_name_then_is_invalid( string topicName )
        {
            MqttTopicEvaluator topicEvaluator = new MqttTopicEvaluator( new MqttConfiguration() );

            Assert.False( topicEvaluator.IsValidTopicName( topicName ) );
        }

        [Theory]
        [TestCase( "foo/bar" )]
        [TestCase( "foo/#" )]
        [TestCase( "#" )]
        [TestCase( "foo/bar/" )]
        [TestCase( "foo/+/+/bar" )]
        [TestCase( "+/bar/test" )]
        [TestCase( "/foo" )]
        [TestCase( "/" )]
        public void when_evaluating_valid_topic_filter_then_is_valid( string topicFilter )
        {
            MqttTopicEvaluator topicEvaluator = new MqttTopicEvaluator( new MqttConfiguration() );

            Assert.True( topicEvaluator.IsValidTopicFilter( topicFilter ) );
        }

        [Theory]
        [TestCase( "foo/#/#" )]
        [TestCase( "foo/bar#/" )]
        [TestCase( "foo/bar+/test" )]
        [TestCase( "" )]
        [TestCase( "foo/#/bar" )]
        public void when_evaluating_invalid_topic_filter_then_is_invalid( string topicFilter )
        {
            MqttTopicEvaluator topicEvaluator = new MqttTopicEvaluator( new MqttConfiguration() );

            Assert.False( topicEvaluator.IsValidTopicFilter( topicFilter ) );
        }

        [Theory]
        [TestCase( "foo/#" )]
        [TestCase( "#" )]
        [TestCase( "foo/+/+/bar" )]
        [TestCase( "+/bar/test" )]
        public void when_evaluating_topic_filter_with_wildcards_and_configuration_does_not_allow_wildcards_then_is_invalid( string topicFilter )
        {
            MqttTopicEvaluator topicEvaluator = new MqttTopicEvaluator( new MqttConfiguration
            {
                AllowWildcardsInTopicFilters = false
            } );

            Assert.False( topicEvaluator.IsValidTopicFilter( topicFilter ) );
        }

        [Theory]
        [TestCase( "sport/tennis/player1/#", "sport/tennis/player1" )]
        [TestCase( "sport/tennis/player1/#", "sport/tennis/player1/ranking" )]
        [TestCase( "sport/tennis/player1/#", "sport/tennis/player1/score/wimbledon" )]
        [TestCase( "sport/#", "sport" )]
        [TestCase( "#", "foo/bar" )]
        [TestCase( "#", "/" )]
        [TestCase( "#", "foo/bar/test" )]
        [TestCase( "#", "foo/bar/" )]
        [TestCase( "games/table tennis/players", "games/table tennis/players" )]
        [TestCase( "games/+/players", "games/table tennis/players" )]
        [TestCase( "#", "games/table tennis/players/ranking" )]
        [TestCase( "Accounts payable", "Accounts payable" )]
        [TestCase( "/", "/" )]
        public void when_matching_valid_topic_name_with_multi_level_wildcard_topic_filter_then_matches( string topicFilter, string topicName )
        {
            MqttTopicEvaluator topicEvaluator = new MqttTopicEvaluator( new MqttConfiguration() );

            Assert.True( topicEvaluator.Matches( topicName, topicFilter ) );
        }

        [Theory]
        [TestCase( "sport/tennis/+", "sport/tennis/player1" )]
        [TestCase( "sport/tennis/+", "sport/tennis/player2" )]
        [TestCase( "sport/+", "sport/" )]
        [TestCase( "+/+", "/finance" )]
        [TestCase( "/+", "/finance" )]
        public void when_matching_valid_topic_name_with_single_level_wildcard_topic_filter_then_matches( string topicFilter, string topicName )
        {
            MqttTopicEvaluator topicEvaluator = new MqttTopicEvaluator( new MqttConfiguration() );

            Assert.True( topicEvaluator.Matches( topicName, topicFilter ) );
        }

        [Theory]
        [TestCase( "+/tennis/#", "sport/tennis/player1/ranking/" )]
        [TestCase( "+/tennis/#", "sport/tennis/player1/ranking/player2" )]
        [TestCase( "+/tennis/#", "sport/tennis" )]
        [TestCase( "+/tennis/#", "sport/tennis/" )]
        [TestCase( "+/tennis/#", "games/tennis/player1/ranking/player2" )]
        [TestCase( "+/foo/+/bar/#", "sport/foo/players/bar" )]
        [TestCase( "+/foo/+/bar/#", "game/foo/ranking/bar/test/player1" )]
        [TestCase( "+/foo/+/bar/#", "game/foo/ranking/bar/test/player1/" )]
        public void when_matching_valid_topic_name_with_mixed_wildcards_topic_filter_then_matches( string topicFilter, string topicName )
        {
            MqttTopicEvaluator topicEvaluator = new MqttTopicEvaluator( new MqttConfiguration() );

            Assert.True( topicEvaluator.Matches( topicName, topicFilter ) );
        }

        [Theory]
        [TestCase( "$SYS/#", "$SYS/foo/bar" )]
        [TestCase( "$SYS/#", "$SYS/foo/bar/test" )]
        [TestCase( "$SYS/#", "$SYS" )]
        [TestCase( "$SYS/#", "$SYS/" )]
        [TestCase( "$SYS/monitor/+", "$SYS/monitor/Clients" )]
        [TestCase( "$/+/test", "$/foo/test" )]
        public void when_matching_reserved_topic_names_then_matches( string topicFilter, string topicName )
        {
            MqttTopicEvaluator topicEvaluator = new MqttTopicEvaluator( new MqttConfiguration() );

            Assert.True( topicEvaluator.Matches( topicName, topicFilter ) );
        }

        [Theory]
        [TestCase( "sport/tennis/+", "sport/tennis/player1/ranking" )]
        [TestCase( "sport/+", "sport" )]
        [TestCase( "+", "/finance" )]
        public void when_matching_invalid_topic_name_with_single_level_wildcard_topic_filter_then_does_not_match( string topicFilter, string topicName )
        {
            MqttTopicEvaluator topicEvaluator = new MqttTopicEvaluator( new MqttConfiguration() );

            Assert.False( topicEvaluator.Matches( topicName, topicFilter ) );
        }

        [Theory]
        [TestCase( "foo/bar/test", "FOO/BAR/TEST" )]
        [TestCase( "foo/bar/test", "foo/bar/Test" )]
        [TestCase( "FOO/BAR/TEST", "FOO/bar/TEST" )]
        [TestCase( "+/BAR/TEST", "FOO/BAR/TESt" )]
        [TestCase( "ACCOUNTS", "Accounts" )]
        public void when_matching_topic_name_and_topic_filter_with_different_case_then_does_not_match( string topicFilter, string topicName )
        {
            MqttTopicEvaluator topicEvaluator = new MqttTopicEvaluator( new MqttConfiguration() );

            Assert.False( topicEvaluator.Matches( topicName, topicFilter ) );
        }

        [Theory]
        [TestCase( "#", "$/test" )]
        [TestCase( "#", "$SYS/test/foo" )]
        [TestCase( "+/monitor/#", "$/monitor/Clients" )]
        [TestCase( "+/monitor/Clients", "$SYS/monitor/Clients" )]
        public void when_matching_reserved_topic_names_with_starting_wildcards_then_does_not_match( string topicFilter, string topicName )
        {
            MqttTopicEvaluator topicEvaluator = new MqttTopicEvaluator( new MqttConfiguration() );

            Assert.False( topicEvaluator.Matches( topicName, topicFilter ) );
        }

        [Theory]
        [TestCase( "foo/+" )]
        [TestCase( "#" )]
        [TestCase( "" )]
        public void when_matching_with_invalid_topic_name_then_fails( string topicName )
        {
            MqttTopicEvaluator topicEvaluator = new MqttTopicEvaluator( new MqttConfiguration() );

            Assert.Throws<MqttException>( () => topicEvaluator.Matches( topicName, "#" ) );
        }

        [Theory]
        [TestCase( "foo/#/#" )]
        [TestCase( "foo/bar#/" )]
        [TestCase( "foo/bar+/test" )]
        [TestCase( "" )]
        [TestCase( "foo/#/bar" )]
        public void when_matching_with_invalid_topic_filter_then_fails( string topicFilter )
        {
            MqttTopicEvaluator topicEvaluator = new MqttTopicEvaluator( new MqttConfiguration() );

            Assert.Throws<MqttException>( () => topicEvaluator.Matches( "foo", topicFilter ) );
        }

        [Theory]
        [TestCase( "test/foo", "test/foo/testClient" )]
        [TestCase( "test", "test/" )]
        [TestCase( "foo/bar/test/", "foo/bar/test" )]
        [TestCase( "test/foo/testClient", "test/foo" )]
        [TestCase( "test/", "test" )]
        [TestCase( "foo/bar/test", "foo/bar/test/" )]
        public void when_matching_not_compatible_topics_then_does_not_match( string topicFilter, string topicName )
        {
            MqttTopicEvaluator topicEvaluator = new MqttTopicEvaluator( new MqttConfiguration() );

            Assert.False( topicEvaluator.Matches( topicName, topicFilter ) );
        }

        [Theory]
        [TestCase( "test/foo", "test/foo" )]
        [TestCase( "test/", "test/" )]
        [TestCase( "foo/bar/test/", "foo/bar/test/" )]
        [TestCase( "test/foo/testClient", "test/foo/testClient" )]
        [TestCase( "test", "test" )]
        [TestCase( "foo/bar/test", "foo/bar/test" )]
        public void when_matching_compatible_topics_then_matches( string topicFilter, string topicName )
        {
            MqttTopicEvaluator topicEvaluator = new MqttTopicEvaluator( new MqttConfiguration() );

            Assert.True( topicEvaluator.Matches( topicName, topicFilter ) );
        }
    }
}
