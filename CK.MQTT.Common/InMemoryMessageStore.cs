using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.MQTT.Common
{
    public class InMemoryMessageStore : IMessageStore
    {
        readonly List<StoredApplicationMessage> _messages = new List<StoredApplicationMessage>( 10 );
        readonly List<ushort> _orphansIds = new List<ushort>();
        public IEnumerable<ushort> OrphansPacketsId => _orphansIds;

        public IEnumerable<StoredApplicationMessage> AllStoredMessages => _messages;

        public ValueTask<QualityOfService> DiscardMessageFromIdAsync( ushort packetId )
        {
            foreach( var msg in _messages )
            {
                if( msg.PacketId == packetId )
                {
                    _messages.Remove( msg );
                    if( msg.QualityOfService == QualityOfService.ExactlyOnce )
                    {
                        _orphansIds.Add( packetId );
                    }
                    return new ValueTask<QualityOfService>( msg.QualityOfService );
                }
            }   
            throw new ArgumentException( "Could not find packet ID in the list." );
        }

        public ValueTask FreePacketIdentifier( ushort packetId )
        {
            if( !_orphansIds.Remove( packetId ) )
            {
                if( _messages.Any( s => s.PacketId == packetId ) )
                {
                    throw new InvalidOperationException( "Message must be discarded first." );
                }
                throw new InvalidOperationException( "Unexistant packet ID." );
            }
            return new ValueTask();
        }


        bool IsPackageIdAvailable( ushort id )
        {
            foreach( StoredApplicationMessage msg in _messages )
            {
                if( msg.PacketId == id ) return false;
            }
            return true;
        }

        readonly object _lock = new object();
        ushort _lastId = 0;
        ushort GetAvailablePackageId()
        {
            if( _messages.Count == ushort.MaxValue ) throw new InvalidOperationException( "Too much message stored." );
            lock( _lock )
            {
                while( !IsPackageIdAvailable( _lastId ) )
                {
                    _lastId++;
                }
                return _lastId;
            }
        }

        public ValueTask<ushort> StoreMessage( ApplicationMessage message, QualityOfService qos )
        {
            ushort id = GetAvailablePackageId();
            _messages.Add( new StoredApplicationMessage( message, qos, id ) );
            return new ValueTask<ushort>( id );
        }
    }
}
