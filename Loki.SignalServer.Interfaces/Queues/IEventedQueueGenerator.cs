using System;
using Loki.SignalServer.Interfaces.Utility;

namespace Loki.SignalServer.Interfaces.Queues
{
    public interface IEventedQueueGenerator
    {
        /// <summary>
        /// Adds the queue.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queueParameters">The queue parameters.</param>
        void AddQueue<T>(IParameterList queueParameters);

        /// <summary>
        /// Adds the dequeue event.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queueId">The queue identifier.</param>
        /// <param name="exchangeId">The exchange identifier.</param>
        /// <param name="func">The function.</param>
        void AddDequeueEvent<T>(string queueId, string exchangeId, EventHandler<T> func);

        /// <summary>
        /// Removes the dequeue event.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queueId">The queue identifier.</param>
        /// <param name="exchangeId">The exchange identifier.</param>
        /// <param name="func">The function.</param>
        void RemoveDequeueEvent<T>(string queueId, string exchangeId, EventHandler<T> func);

        /// <summary>
        /// Enqueues the specified queue identifier.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queueId">The queue identifier.</param>
        /// <param name="exchangeId">The exchange identifier.</param>
        /// <param name="item">The item.</param>
        void Enqueue<T>(string queueId, string exchangeId, T item);
    }
}