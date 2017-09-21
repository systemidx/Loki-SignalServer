using System;
using System.Collections.Concurrent;
using Loki.SignalServer.Interfaces.Queues;

namespace Loki.SignalServer.Common.Queues
{
    public class EventedQueueParameters : IEventedQueueParameters
    {
        private readonly ConcurrentDictionary<string,dynamic> _backingStore = new ConcurrentDictionary<string, dynamic>();

        public dynamic this[string index]
        {
            get {
                if (_backingStore.ContainsKey(index))
                    return _backingStore[index];

                throw new IndexOutOfRangeException(nameof(index));
            }
            set
            {
                int i = 0;

                while (i < 3) { 
                    _backingStore.TryAdd(index, value);
                    ++i;
                }
            }
        }
    }
}