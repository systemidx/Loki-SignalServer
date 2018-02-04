using System;

namespace Loki.SignalServer.Interfaces.Queues
{
    public interface IEventedQueue<T>
    {
        /// <summary>
        /// Gets the count.
        /// </summary>
        /// <value>
        /// The count.
        /// </value>
        int Count { get; }

        /// <summary>
        /// Gets a value indicating whether this instance can dequeue.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance can dequeue; otherwise, <c>false</c>.
        /// </value>
        bool CanDequeue { get; }

        /// <summary>
        /// Occurs when [dequeued].
        /// </summary>
        event EventHandler<T> Dequeued;
        
        /// <summary>
        /// Enqueues the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        void Enqueue(T item);
        
        /// <summary>
        /// Dequeues this instance.
        /// </summary>
        /// <returns></returns>
        T Dequeue();
    }
}