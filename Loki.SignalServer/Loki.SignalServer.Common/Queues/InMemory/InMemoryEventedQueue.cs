using System;
using System.Collections.Concurrent;
using Loki.SignalServer.Interfaces.Queues;

namespace Loki.SignalServer.Common.Queues.InMemory
{
    public class InMemoryEventedQueue<T> : IEventedQueue<T>
    {
        private readonly ConcurrentQueue<T> _backingQueue = new ConcurrentQueue<T>();

        public int Count => _backingQueue.Count;
        public bool CanDequeue => true;

        public event EventHandler<T> Dequeued;
        
        public void Enqueue(T item)
        {
            _backingQueue.Enqueue(item);
        }

        public T Dequeue()
        {
            _backingQueue.TryDequeue(out T result);

            Dequeued?.Invoke(this, result);

            return result;
        }
    }
}
