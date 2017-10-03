using System;
using System.Collections.Concurrent;
using Loki.SignalServer.Interfaces.Queues;

namespace Loki.SignalServer.Common.Queues.InMemory
{
    public class InMemoryEventedQueue<T> : IEventedQueue<T>
    {
        #region Readonly Variables

        /// <summary>
        /// The backing queue
        /// </summary>
        private readonly ConcurrentQueue<T> _backingQueue = new ConcurrentQueue<T>();

        #endregion

        #region Properties

        /// <summary>
        /// Gets the count.
        /// </summary>
        /// <value>
        /// The count.
        /// </value>
        public int Count => _backingQueue.Count;

        /// <summary>
        /// Gets a value indicating whether this instance can dequeue.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance can dequeue; otherwise, <c>false</c>.
        /// </value>
        public bool CanDequeue => true;

        /// <summary>
        /// Gets or sets the parameters.
        /// </summary>
        /// <value>
        /// The parameters.
        /// </value>
        public IEventedQueueParameters Parameters { get; set; }

        #endregion

        #region Member Variables

        /// <summary>
        /// Occurs when [dequeued].
        /// </summary>
        public event EventHandler<T> Dequeued;

        #endregion

        #region Public Methods

        /// <summary>
        /// Enqueues the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void Enqueue(T item)
        {
            _backingQueue.Enqueue(item);
        }

        /// <summary>
        /// Dequeues this instance.
        /// </summary>
        /// <returns></returns>
        public T Dequeue()
        {
            _backingQueue.TryDequeue(out T result);

            Dequeued?.Invoke(this, result);

            return result;
        }

        #endregion
    }
}
