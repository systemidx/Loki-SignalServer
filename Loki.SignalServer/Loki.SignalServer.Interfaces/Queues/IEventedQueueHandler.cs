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
        /// <param name="parameters">The parameters.</param>
        IEventedQueue<T> CreateQueue(IEventedQueueParameters parameters);
    }
}