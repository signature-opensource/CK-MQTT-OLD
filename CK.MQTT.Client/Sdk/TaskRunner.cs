using System;
using System.Threading;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk
{
    internal class TaskRunner : IDisposable
    {
        TaskFactory _taskFactory;
        bool _disposed;

        private TaskRunner()
        {
            _taskFactory = new TaskFactory( CancellationToken.None,
                TaskCreationOptions.DenyChildAttach,
                TaskContinuationOptions.None,
                new SingleThreadScheduler() );
        }

        public static TaskRunner Get() => new TaskRunner();

        public Task Run( Func<Task> func )
        {
            if( _disposed )
            {
                throw new ObjectDisposedException( GetType().FullName );
            }

            return _taskFactory.StartNew( func ).Unwrap();
        }

        public Task<T> Run<T>( Func<Task<T>> func )
        {
            if( _disposed )
            {
                throw new ObjectDisposedException( GetType().FullName );
            }

            return _taskFactory.StartNew( func ).Unwrap();
        }

        public Task Run( Action action )
        {
            if( _disposed )
            {
                throw new ObjectDisposedException( GetType().FullName );
            }

            return _taskFactory.StartNew( action );
        }

        public Task<T> Run<T>( Func<T> func )
        {
            if( _disposed )
            {
                throw new ObjectDisposedException( GetType().FullName );
            }

            return _taskFactory.StartNew( func );
        }

        public void Dispose()
        {
            if( _disposed ) return;

            (_taskFactory.Scheduler as IDisposable)?.Dispose();
            _taskFactory = null;
            _disposed = true;
        }
    }
}
