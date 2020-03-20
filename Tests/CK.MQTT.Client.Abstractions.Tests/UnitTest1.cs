using CK.Core;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

namespace CK.MQTT.Client.Abstractions.Tests
{
    class TestImpl : IMqttClient
    {
        SequentialEventHandlerSender<MqttApplicationMessage> _eSeqMessage;
        SequentialEventHandlerAsyncSender<MqttApplicationMessage> _eSeqMessageAsync;
        ParallelEventHandlerAsyncSender<MqttApplicationMessage> _eParMessageAsync;

        SequentialEventHandlerSender<MqttEndpointDisconnected> _eSeqDisconnect;
        SequentialEventHandlerAsyncSender<MqttEndpointDisconnected> _eSeqDisconnectAsync;
        ParallelEventHandlerAsyncSender<MqttEndpointDisconnected> _eParDisconnectAsync;

        public TestImpl( IActivityMonitor monitor )
        {
            _eSeqMessage = new SequentialEventHandlerSender<MqttApplicationMessage>();
            _eSeqMessageAsync = new SequentialEventHandlerAsyncSender<MqttApplicationMessage>();
            _eParMessageAsync = new ParallelEventHandlerAsyncSender<MqttApplicationMessage>();

            _eSeqDisconnect = new SequentialEventHandlerSender<MqttEndpointDisconnected>();
            _eSeqDisconnectAsync = new SequentialEventHandlerAsyncSender<MqttEndpointDisconnected>();
            _eParDisconnectAsync = new ParallelEventHandlerAsyncSender<MqttEndpointDisconnected>();

            Monitor = monitor;
        }
        public IActivityMonitor Monitor { get; }

        /// <summary>
        /// This is THE sender: first, async parallels are launched, then the synchronous ones and then sequential async ones
        /// and we wait for the parallels to complete...
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="payload"></param>
        /// <returns></returns>
        public Task DoReceiveMessageAsync( string topic, byte[] payload )
        {
            var msg = new MqttApplicationMessage( topic, payload );
            var taskParallel = _eParMessageAsync.RaiseAsync( Monitor, this, msg );
            _eSeqMessage.Raise( Monitor, this, msg );
            return Task.WhenAll( _eSeqMessageAsync.RaiseAsync( Monitor, this, msg ), taskParallel );
        }

        public async Task DoSafeReceiveMessageAsync( string topic, byte[] payload )
        {
            try
            {
                var msg = new MqttApplicationMessage( topic, payload );
                var taskParallel = _eParMessageAsync.RaiseAsync( Monitor, this, msg );
                _eSeqMessage.Raise( Monitor, this, msg );
                await _eSeqMessageAsync.RaiseAsync( Monitor, this, msg );
                await taskParallel;
            }
            catch( Exception ex )
            {
                Monitor.Error( ex );
            }
        }

        public event SequentialEventHandler<MqttApplicationMessage> MessageReceived
        {
            add { _eSeqMessage.Add( value ); }
            remove { _eSeqMessage.Remove( value ); }
        }

        public event SequentialEventHandlerAsync<MqttApplicationMessage> MessageReceivedAsync
        {
            add { _eSeqMessageAsync.Add( value ); }
            remove { _eSeqMessageAsync.Remove( value ); }
        }

        public event ParallelEventHandlerAsync<MqttApplicationMessage> ParallelMessageReceivedAsync
        {
            add { _eParMessageAsync.Add( value ); }
            remove { _eParMessageAsync.Remove( value ); }
        }

        public event SequentialEventHandler<MqttEndpointDisconnected> Disconnected
        {
            add { _eSeqDisconnect.Add( value ); }
            remove { _eSeqDisconnect.Remove( value ); }
        }

        public event SequentialEventHandlerAsync<MqttEndpointDisconnected> DisconnectedAsync
        {
            add { _eSeqDisconnectAsync.Add( value ); }
            remove { _eSeqDisconnectAsync.Remove( value ); }
        }

        public event ParallelEventHandlerAsync<MqttEndpointDisconnected> ParallelDisconnectedAsync
        {
            add { _eParDisconnectAsync.Add( value ); }
            remove { _eParDisconnectAsync.Remove( value ); }
        }

        public string ClientId => throw new NotImplementedException();

        public bool CheckConnection( IActivityMonitor m )
        {
            throw new NotImplementedException();
        }

        public Task<SessionState> ConnectAsync( IActivityMonitor m, MqttClientCredentials credentials = null, MqttLastWill will = null, bool cleanSession = false )
        {
            throw new NotImplementedException();
        }

        public Task DisconnectAsync( IActivityMonitor m )
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
        }

        public Task PublishAsync( IActivityMonitor m, string topic, ReadOnlyMemory<byte> payload, MqttQualityOfService qos, bool retain = false )
        {
            throw new NotImplementedException();
        }

        public Task SubscribeAsync( IActivityMonitor m, string topicFilter, MqttQualityOfService qos )
        {
            throw new NotImplementedException();
        }

        public Task UnsubscribeAsync( IActivityMonitor m, IEnumerable<string> topics )
        {
            throw new NotImplementedException();
        }
    }


    public class Tests
    {
        class MqttCientConsumer
        {
            readonly IActivityMonitor _monitor;
            readonly string _name;
            readonly Random _rand;

            public MqttCientConsumer( string name )
            {
                _monitor = new ActivityMonitor( "MqttCientConsumer" );
                _name = name;
                _rand = new Random( name.GetHashCode() );
            }

            public string LastTopic { get; private set; }

            public async Task OnMessage( ActivityMonitor.DependentToken token, IMqttClient client, MqttApplicationMessage m )
            {
                using( _monitor.StartDependentActivity( token ) )
                {
                    _monitor.Info( $"Consumer {_name}: Receiving message from topic '{m.Topic}'." );
                    await Task.Delay( _rand.Next( 20, 1000 ) );
                    _monitor.Info( $"Consumer {_name} did it job." );
                    LastTopic = m.Topic;
                }
            }

            public async Task OnDisconnect( ActivityMonitor.DependentToken token, IMqttClient client, MqttEndpointDisconnected m )
            {
                using( _monitor.StartDependentActivity( token ) )
                {
                    _monitor.Info( $"Consumer {_name}: Disconnection from {client.ClientId}: Message = '{m.Message}'." );
                    await Task.Delay( _rand.Next( 20, 150 ) );
                    LastTopic = null;
                }
            }
        }

        [Test]
        public async Task event_async_and_sync_look_the_same()
        {
            using var impl = new TestImpl( TestHelper.Monitor );
            
            var consumer1 = new MqttCientConsumer( "Consumer1" );
            var consumer2 = new MqttCientConsumer( "Consumer2" );
            var consumer3 = new MqttCientConsumer( "Consumer3" );

            impl.MessageReceived += SyncReceiving;
            impl.MessageReceivedAsync += SequentialAsyncReceiving;
            impl.ParallelMessageReceivedAsync += consumer1.OnMessage;
            impl.ParallelMessageReceivedAsync += consumer2.OnMessage;
            impl.ParallelMessageReceivedAsync += consumer3.OnMessage;

            await impl.DoReceiveMessageAsync( "topic", Array.Empty<byte>() );
            LastTopicSync.Should().Be( "topic" );
            LastTopicASync.Should().Be( "topic" );
            consumer1.LastTopic.Should().Be( "topic" );
            consumer2.LastTopic.Should().Be( "topic" );
            consumer3.LastTopic.Should().Be( "topic" );

            await impl.DoReceiveMessageAsync( "topic2", Array.Empty<byte>() );
            LastTopicSync.Should().Be( "topic2" );
            LastTopicASync.Should().Be( "topic2" );
            consumer1.LastTopic.Should().Be( "topic2" );
            consumer2.LastTopic.Should().Be( "topic2" );
            consumer3.LastTopic.Should().Be( "topic2" );

            impl.MessageReceivedAsync -= SequentialAsyncReceiving;
            await impl.DoReceiveMessageAsync( "topic3", Array.Empty<byte>() );
            LastTopicSync.Should().Be( "topic3" );
            LastTopicASync.Should().Be( "topic2" );
            consumer1.LastTopic.Should().Be( "topic3" );
            consumer2.LastTopic.Should().Be( "topic3" );
            consumer3.LastTopic.Should().Be( "topic3" );

            impl.MessageReceivedAsync += SequentialAsyncReceiving;
            await impl.DoReceiveMessageAsync( "topic4", Array.Empty<byte>() );
            LastTopicSync.Should().Be( "topic4" );
            LastTopicASync.Should().Be( "topic4" );
            consumer1.LastTopic.Should().Be( "topic4" );
            consumer2.LastTopic.Should().Be( "topic4" );
            consumer3.LastTopic.Should().Be( "topic4" );
        }

        static string LastTopicSync;
        static string LastTopicASync;

        private static void SyncReceiving( IActivityMonitor monitor, IMqttClient sender, MqttApplicationMessage e )
        {
            LastTopicSync = e.Topic;
            monitor.Info( "Synchronous Reception." );
        }

        static Task SequentialAsyncReceiving( IActivityMonitor monitor, IMqttClient sender, MqttApplicationMessage e )
        {
            LastTopicASync = e.Topic;
            monitor.Info( "Asynchronous Reception (but sequential)." );
            return Task.CompletedTask;
        }
    }
}
