namespace CK.MQTT.Sdk
{
    internal class PacketIdProvider : IPacketIdProvider
    {
        readonly object _lockObject;
        volatile ushort _lastValue;

        public PacketIdProvider()
        {
            _lockObject = new object();
            _lastValue = 0;
        }

        public ushort GetPacketId()
        {
            ushort id = default;

            lock( _lockObject )
            {
                if( _lastValue == ushort.MaxValue )
                {
                    id = 1;
                }
                else
                {
                    id = (ushort)(_lastValue + 1);
                }

                _lastValue = id;
            }

            return id;
        }
    }
}
