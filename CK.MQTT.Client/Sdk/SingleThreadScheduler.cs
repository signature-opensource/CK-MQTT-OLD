using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk
{
    internal sealed class SingleThreadScheduler : TaskScheduler, IDisposable
    {
        BlockingCollection<Task> _tasks;
        readonly Task _runner;

        public SingleThreadScheduler()
        {
            _tasks = new BlockingCollection<Task>();
            _runner = new Task( () =>
            {
                if( _tasks == null )
                {
                    return;
                }

                foreach( Task task in _tasks.GetConsumingEnumerable() )
                {
                    TryExecuteTask( task );
                }
            } );

            _runner.Start( Default );
        }

        public override int MaximumConcurrencyLevel => 1;

        protected override void QueueTask( Task task )
        {
            _tasks.Add( task );
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return _tasks.ToArray();
        }

        protected override bool TryExecuteTaskInline( Task task, bool taskWasPreviouslyQueued )
        {
            return TryExecuteTask( task );
        }

        public void Dispose()
        {
            if( _tasks != null )
            {
                _tasks.CompleteAdding();
                _tasks = null;
            }
        }
    }
}
