using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace CK.MQTT.Sdk.Bindings
{
    internal enum EndpointIdentifier
    {
        Server,
        Client
    }

    internal class PrivateStream : IDisposable
    {
        bool _disposed;
        readonly ReplaySubject<Tuple<byte[], EndpointIdentifier>> payloadSequence;

        public PrivateStream( MqttConfiguration configuration )
        {
            payloadSequence = new ReplaySubject<Tuple<byte[], EndpointIdentifier>>( window: TimeSpan.FromSeconds( configuration.WaitTimeoutSecs ) );
        }

        public bool IsDisposed => payloadSequence.IsDisposed;

        public IObservable<byte[]> Receive( EndpointIdentifier identifier )
        {
            if( _disposed ) throw new ObjectDisposedException( nameof( PrivateStream ) );

            return payloadSequence
                .Where( t => t.Item2 == identifier )
                .Select( t => t.Item1 );
        }

        public void Send( byte[] payload, EndpointIdentifier identifier )
        {
            if( _disposed ) throw new ObjectDisposedException( nameof( PrivateStream ) );
            payloadSequence.OnNext( Tuple.Create( payload, identifier ) );
        }

        public void Dispose()
        {
            if( _disposed ) return;

            payloadSequence.OnCompleted();
            payloadSequence.Dispose();
            _disposed = true;
        }
    }
}
