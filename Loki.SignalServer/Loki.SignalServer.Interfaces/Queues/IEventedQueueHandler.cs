using System;

namespace Loki.SignalServer.Interfaces.Queues
{
    public interface IEventedQueueHandler<T>
    {
        /// <summary>
        /// Gets a value indicating whether this instance is running.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is running; otherwise, <c>false</c>.
        /// </value>
        bool IsRunning { get; }

        /// <summary>
        /// Starts this instance.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops this instance.
        /// </summary>
        void Stop();

        /// <summary>
        /// Creates the queue.
        /// </summary>
        /// <param name="exchangeId">The exchange identifier.</param>
        /// <param name="queueId">The identifier.</param>
        void CreateQueue(string exchangeId, string queueId);

        /// <summary>
        /// Removes the queue.
        /// </summary>
        /// <param name="exchangeId">The exchange identifier.</param>
        /// <param name="queueId">The queue identifier.</param>
        void RemoveQueue(string exchangeId, string queueId);

        /// <summary>
        /// Adds the event.
        /// </summary>
        /// <param name="exchangeId">The exchange identifier.</param>
        /// <param name="queueId">The queue identifier.</param>
        /// <param name="eventHandler">The event handler.</param>
        void AddEvent(string exchangeId, string queueId, EventHandler<T> eventHandler);

        /// <summary>
        /// Removes the event.
        /// </summary>
        /// <param name="exchangeId">The exchange identifier.</param>
        /// <param name="queueId">The queue identifier.</param>
        /// <param name="eventHandler">The event handler.</param>
        void RemoveEvent(string exchangeId, string queueId, EventHandler<T> eventHandler);

        /// <summary>
        /// Enqueues the specified queue identifier.
        /// </summary>
        /// <param name="exchangeId">The exchange identifier.</param>
        /// <param name="queueId">The queue identifier.</param>
        /// <param name="obj">The object.</param>
        void Enqueue(string exchangeId, string queueId, T obj);
    }
}