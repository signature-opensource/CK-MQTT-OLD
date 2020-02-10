using System;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk
{
    internal class Timer
    {
        volatile int _intervalMillisecs;
        volatile bool _isRunning;

        public Timer() : this( intervalMillisecs: 0 )
        {
        }

        public Timer( int intervalMillisecs, bool autoReset = true )
        {
            IntervalMillisecs = intervalMillisecs;
            AutoReset = autoReset;
        }

        public event EventHandler Elapsed = ( sender, e ) => { };

        public int IntervalMillisecs
        {
            get => _intervalMillisecs;
            set => _intervalMillisecs = value;
        }

        public bool AutoReset { get; set; }

        public void Start()
        {
            if( _isRunning ) return;
            if( IntervalMillisecs <= 0 ) throw new InvalidOperationException();
            _isRunning = true;
        }

        public void Reset()
        {
            Stop();
            Start();
        }

        public void Stop()
        {
            _isRunning = false;
        }
    }
}
