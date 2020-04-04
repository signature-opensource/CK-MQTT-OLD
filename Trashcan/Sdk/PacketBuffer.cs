using System.Collections.Generic;
using System.Linq;

namespace CK.MQTT.Sdk
{
    public class PacketBuffer : IPacketBuffer
    {
        bool _packetReadStarted;
        bool _packetRemainingLengthReadCompleted;
        int _packetRemainingLength = 0;
        bool _isPacketReady;

        readonly object _bufferLock = new object();
        readonly IList<byte> _mainBuffer;
        readonly IList<byte> _pendingBuffer;

        public PacketBuffer()
        {
            _mainBuffer = new List<byte>();
            _pendingBuffer = new List<byte>();
        }

        public bool TryGetPackets( IEnumerable<byte> sequence, out IEnumerable<byte[]> packets )
        {
            List<byte[]> result = new List<byte[]>();

            lock( _bufferLock )
            {
                Buffer( sequence );

                while( _isPacketReady )
                {
                    byte[] packet = _mainBuffer.ToArray();

                    result.Add( packet );
                    Reset();
                }

                packets = result;
            }

            return result.Any();
        }

        void Buffer( IEnumerable<byte> sequence )
        {
            foreach( byte @byte in sequence )
            {
                Buffer( @byte );
            }
        }

        void Buffer( byte @byte )
        {
            if( _isPacketReady )
            {
                _pendingBuffer.Add( @byte );
                return;
            }

            _mainBuffer.Add( @byte );

            if( !_packetReadStarted )
            {
                _packetReadStarted = true;
                return;
            }

            if( !_packetRemainingLengthReadCompleted )
            {
                if( (@byte & 128) == 0 )
                {
                    _packetRemainingLength = MqttProtocol.Encoding.DecodeRemainingLength( _mainBuffer.ToArray(), out _ );
                    _packetRemainingLengthReadCompleted = true;

                    if( _packetRemainingLength == 0 )
                        _isPacketReady = true;
                }

                return;
            }

            if( _packetRemainingLength == 1 )
            {
                _isPacketReady = true;
            }
            else
            {
                _packetRemainingLength--;
            }
        }

        void Reset()
        {
            _mainBuffer.Clear();
            _packetReadStarted = false;
            _packetRemainingLengthReadCompleted = false;
            _packetRemainingLength = 0;
            _isPacketReady = false;

            if( _pendingBuffer.Any() )
            {
                byte[] pendingSequence = _pendingBuffer.ToArray();

                _pendingBuffer.Clear();
                Buffer( pendingSequence );
            }
        }
    }
}
