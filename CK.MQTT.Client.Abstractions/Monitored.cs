using System;
using System.Runtime.CompilerServices;

namespace CK.Core
{
    public static class Mon
    {
        public static Mon<TOut> Cast<TOut, TIn>( this in Mon<TIn> other ) where TOut : TIn
            => new Mon<TOut>( other.Monitor, (TOut)other.Item );

        public static Mon<TOut> From<TOut, TIn>( this in Mon<TIn> other ) where TIn : TOut
            => new Mon<TOut>( other.Monitor, other.Item );

        public static IObservable<Mon<TOut>> OfMonitoredType<TOut, TIn>( this IObservable<Mon<TIn>> @this )
            where TOut : TIn
            => @this.Where( p => p.Item is TOut )
                .Select( s => s.Cast<TOut, TIn>() );
    }

    public readonly struct Mon<T>
    {
        public Mon( IActivityMonitor monitor, T item )
        {
            Monitor = monitor;
            Item = item;
        }

        public IActivityMonitor Monitor { get; }

        public T Item { get; }
    }
}
