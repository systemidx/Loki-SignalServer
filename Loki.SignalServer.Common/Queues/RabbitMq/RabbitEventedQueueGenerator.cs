using System;
using Loki.Interfaces.Dependency;
using Loki.Interfaces.Logging;
using Loki.SignalServer.Interfaces.Cache;
using Loki.SignalServer.Interfaces.Queues;
using Loki.SignalServer.Interfaces.Utility;
using RabbitMQ.Client;

namespace Loki.SignalServer.Common.Queues.RabbitMq
{
    public class RabbitEventedQueueGenerator : IEventedQueueGenerator
    {
        #region Private Readonly Variables

        /// <summary>
        /// The dependency utility
        /// </summary>
        private readonly IDependencyUtility _dependencyUtility;

        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The queue cache
        /// </summary>
        private readonly ICache _queueCache;

        /// <summary>
        /// The connection
        /// </summary>
        private readonly IConnection _connection;

        /// <summary>
        /// The channel lock
        /// </summary>
        private readonly object _connectionLock = new object();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="RabbitEventedQueueGenerator"/> class.
        /// </summary>
        /// <param name="dependencyUtility">The dependency utility.</param>
        /// <param name="connectionParameters">The connection parameters.</param>
        /// <param name="connection">The connection.</param>
        public RabbitEventedQueueGenerator(IDependencyUtility dependencyUtility, IParameterList connectionParameters, IConnection connection = null)
        {
            _dependencyUtility = dependencyUtility;
            _queueCache = _dependencyUtility.Resolve<ICacheHandler>().AddCache("rabbitmq-queues", CacheService.InMemory, -1);
            _connection = connection ?? RabbitConnectionGenerator.CreateConnection(_dependencyUtility, connectionParameters);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds the queue.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queueParameters">The queue parameters.</param>
        public void AddQueue<T>(IParameterList queueParameters)
        {
            string exchangeId = queueParameters["ExchangeId"];
            string queueId = queueParameters["QueueId"];
            string routingKey = queueParameters["RouteKey"];

            bool queueDurable = queueParameters["Durable"];
            bool queueTransient = queueParameters["Transient"];
            bool queueAutoDelete = queueParameters["AutoDelete"];

            IModel channel = null;

            lock (_connectionLock)
                channel = _connection.CreateModel();
            
            channel.QueueDeclare(queueId, queueDurable, queueTransient, queueAutoDelete, null);
            channel.QueueBind(queueId, exchangeId, routingKey);

            IEventedQueue<T> queue = new RabbitEventedQueue<T>(exchangeId, queueId, routingKey, channel, _dependencyUtility);
            _queueCache.Set($"{exchangeId}/{queueId}", queue);
        }

        /// <summary>
        /// Adds the dequeue event.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queueId">The queue identifier.</param>
        /// <param name="exchangeId">The exchange identifier.</param>
        /// <param name="func">The function.</param>
        public void AddDequeueEvent<T>(string queueId, string exchangeId, EventHandler<T> func)
        {
            IEventedQueue<T> queue = _queueCache.Get<IEventedQueue<T>>($"{exchangeId}/{queueId}");
            if (queue == null)
                return;

            queue.Dequeued += func;
        }

        /// <summary>
        /// Removes the dequeue event.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queueId">The queue identifier.</param>
        /// <param name="exchangeId">The exchange identifier.</param>
        /// <param name="func">The function.</param>
        public void RemoveDequeueEvent<T>(string queueId, string exchangeId, EventHandler<T> func)
        {
            IEventedQueue<T> queue = _queueCache.Get<IEventedQueue<T>>($"{exchangeId}/{queueId}");
            if (queue == null)
                return;

            queue.Dequeued -= func;
        }

        /// <summary>
        /// Enqueues the specified queue identifier.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queueId">The queue identifier.</param>
        /// <param name="exchangeId">The exchange identifier.</param>
        /// <param name="item">The item.</param>
        public void Enqueue<T>(string queueId, string exchangeId, T item)
        {
            IEventedQueue<T> queue = _queueCache.Get<IEventedQueue<T>>($"{exchangeId}/{queueId}");
            queue?.Enqueue(item);
        }

        #endregion
    }
}
