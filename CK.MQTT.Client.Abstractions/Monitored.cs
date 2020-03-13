using System;
using System.Reactive.Linq;
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

    //public interface Mon<out T>
    //{
    //    IActivityMonitor Monitor { get; }
    //    T Item { get; }
    //}

    //public static class MonExtenstions
    //{
    //    public static IObservable<Mon<TOut>> OfMonitoredType<TOut, IPacket>( this IObservable<Mon<object>> @this )
    //        => @this.Where( p => p.Item is TOut )
    //            .Select( s => new Mon<TOut>( s.Monitor, (TOut)s.Item ) );

    //    public static IObservable<Mon<TOut>> OfMonitoredType<TOut, TIn>( this IObservable<Mon<TIn>> @this )
    //        where TOut : TIn
    //        => @this.Where( p => p.Item is TOut )
    //            .Select( s => (Mon<TOut>)s );
    //}

    //public class Mon<T> : Mon<T>
    //{
    //    Mon( IActivityMonitor monitor, T item )
    //    {
    //        Monitor = monitor;
    //        Item = item;
    //    }

    //    public static Mon<T> Create( IActivityMonitor m, T item ) => new Mon<T>( m, item );

    //    public IActivityMonitor Monitor { get; }

    //    public T Item { get; }
    //}
}
