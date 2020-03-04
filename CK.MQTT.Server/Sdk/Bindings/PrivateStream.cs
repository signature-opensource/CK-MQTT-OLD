
using CK.Core;
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
        readonly ReplaySubject<Tuple<Monitored<byte[]>, EndpointIdentifier>> _payloadSequence;

        public PrivateStream( MqttConfiguration configuration )
        {
            _payloadSequence = new ReplaySubject<Tuple<Monitored<byte[]>, EndpointIdentifier>>( window: TimeSpan.FromSeconds( configuration.WaitTimeoutSecs ) );
        }

        public bool IsDisposed => _payloadSequence.IsDisposed;

        public IObservable<Monitored<byte[]>> Receive( EndpointIdentifier identifier )
        {
            if( _disposed ) throw new ObjectDisposedException( nameof( PrivateStream ) );

            return _payloadSequence
                .Where( t => t.Item2 == identifier )
                .Select( t => t.Item1 );
        }

        public void Send( Monitored<byte[]> payload, EndpointIdentifier identifier )
        {
            if( _disposed ) throw new ObjectDisposedException( nameof( PrivateStream ) );
            _payloadSequence.OnNext( Tuple.Create( payload, identifier ) );
        }

        public void Dispose()
        {
            if( _disposed ) return;

            _payloadSequence.OnCompleted();
            _payloadSequence.Dispose();
            _disposed = true;
        }
    }
}
