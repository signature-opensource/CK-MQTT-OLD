using System;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;

namespace CK.Core
{
    public static class Mon
    {
        public static Mon<T> From<T, TOther>( this in Mon<TOther> other ) where T : TOther
            => new Mon<T>( other.Monitor, (T)other.Item );

        //public static IObservable<Mon<TOut>> OfMonitoredType<TOut, IPacket>( this IObservable<Mon<object>> @this )
        //    => @this.Where( p => p.Item is TOut )
        //        .Select( s => s.From<TOut, object>() );

        public static IObservable<Mon<TOut>> OfMonitoredType<TOut, TIn>( this IObservable<Mon<TIn>> @this )
            where TOut : TIn
            => @this.Where( p => p.Item is TOut )
                .Select( s => s.From<TOut, TIn>() );
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
