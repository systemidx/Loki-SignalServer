using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Dapper;
using Loki.Interfaces.Dependency;
using Loki.Interfaces.Logging;
using Loki.Interfaces.Threading;
using Loki.SignalServer.Common.Enum;
using Loki.SignalServer.Common.Queues.InMemory;
using Loki.SignalServer.Common.Queues.RabbitMq;
using Loki.SignalServer.Interfaces.Configuration;
using Loki.SignalServer.Interfaces.Queues;
using RabbitMQ.Client;

namespace Loki.SignalServer.Common.Queues
{
    public class EventedQueueHandler<T> : IEventedQueueHandler<T>
    {
        #region Readonly Variables

        /// <summary>
        /// The dependency utility
        /// </summary>
        private readonly IDependencyUtility _dependencyUtility;

        /// <summary>
        /// The queues to process
        /// </summary>
        private readonly ConcurrentQueue<string> _queuesToProcess = new ConcurrentQueue<string>();

        /// <summary>
        /// The queues
        /// </summary>
        private readonly ConcurrentDictionary<string, IEventedQueue<T>> _queues = new ConcurrentDictionary<string, IEventedQueue<T>>();
        
        #endregion

        #region Private Variables

        /// <summary>
        /// The queue service
        /// </summary>
        private QueueService _queueService;

        /// <summary>
        /// The processing thread
        /// </summary>
        private Thread _processingThread;

        /// <summary>
        /// The thread helper
        /// </summary>
        private IThreadHelper _threadHelper;

        /// <summary>
        /// The logger
        /// </summary>
        private ILogger _logger;

        /// <summary>
        /// The configuration
        /// </summary>
        private IConfigurationHandler _config;

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether this instance is running.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is running; otherwise, <c>false</c>.
        /// </value>
        public bool IsRunning { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="EventedQueueHandler{T}"/> class.
        /// </summary>
        /// <param name="dependencyUtility">The dependency utility.</param>
        public EventedQueueHandler(IDependencyUtility dependencyUtility)
        {
            _dependencyUtility = dependencyUtility;
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        /// <summary>
        /// Starts this instance.
        /// </summary>
        public void Start()
        {
            IsRunning = true;
            
            _threadHelper = _dependencyUtility.Resolve<IThreadHelper>();
            _logger = _dependencyUtility.Resolve<ILogger>();
            _config = _dependencyUtility.Resolve<IConfigurationHandler>();

            _queueService = _config.Get<QueueService>("queue:service");
            _processingThread = _threadHelper.CreateAndRun(Process);
        }

        /// <inheritdoc />
        /// <summary>
        /// Stops this instance.
        /// </summary>
        public void Stop()
        {
            IsRunning = false;
        }

        /// <summary>
        /// Creates the queue.
        /// </summary>
        /// <param name="exchangeId">The exchange identifier.</param>
        /// <param name="queueId">The identifier.</param>
        /// <param name="exchangeType">Type of the exchange.</param>
        /// <exception cref="T:System.ArgumentException">key</exception>
        public void CreateQueue(string exchangeId, string queueId, string exchangeType = ExchangeType.Direct)
        {
            string key = GetKey(exchangeId, queueId);

            if (_queues.ContainsKey(key))
                return;

            switch (_queueService)
            {
                case QueueService.InMemory:
                    _queues[key] = new InMemoryEventedQueue<T>();
                    break;

                case QueueService.RabbitMq:
                    _queues[key] = new RabbitEventedQueue<T>(exchangeId, queueId, _dependencyUtility, exchangeType);
                    break;

                default:
                    throw new InvalidOperationException(nameof(CreateQueue));
            }
        }

        /// <summary>
        /// Removes the queue.
        /// </summary>
        /// <param name="exchangeId">The exchange identifier.</param>
        /// <param name="queueId">The queue identifier.</param>
        /// <exception cref="ArgumentException">key</exception>
        public void RemoveQueue(string exchangeId, string queueId)
        {
            string key = GetKey(exchangeId, queueId);

            if (!_queues.ContainsKey(key))
                throw new ArgumentException(nameof(key));

            _queues.TryRemove(key, out _);
        }

        /// <summary>
        /// Adds the event.
        /// </summary>
        /// <param name="exchangeId">The exchange identifier.</param>
        /// <param name="queueId">The queue identifier.</param>
        /// <param name="eventHandler">The event handler.</param>
        /// <exception cref="ArgumentException">key</exception>
        public void AddEvent(string exchangeId, string queueId, EventHandler<T> eventHandler)
        {
            string key = GetKey(exchangeId, queueId);

            if (!_queues.ContainsKey(key))
                throw new ArgumentException(nameof(key));

            _queues[key].Dequeued += eventHandler;
        }

        /// <summary>
        /// Removes the event.
        /// </summary>
        /// <param name="exchangeId">The exchange identifier.</param>
        /// <param name="queueId">The queue identifier.</param>
        /// <param name="eventHandler">The event handler.</param>
        /// <exception cref="ArgumentException">key</exception>
        public void RemoveEvent(string exchangeId, string queueId, EventHandler<T> eventHandler)
        {
            string key = GetKey(exchangeId, queueId);

            if (!_queues.ContainsKey(key))
                throw new ArgumentException(nameof(key));

            _queues[key].Dequeued -= eventHandler;
        }

        /// <summary>
        /// Enqueues the specified queue identifier.
        /// </summary>
        /// <param name="exchangeId">The exchange identifier.</param>
        /// <param name="queueId">The queue identifier.</param>
        /// <param name="obj">The object.</param>
        public void Enqueue(string exchangeId, string queueId, T obj)
        {
            string key = GetKey(exchangeId, queueId);

            if (!_queues.ContainsKey(key))
                CreateQueue(exchangeId, queueId);

            _queues[key].Enqueue(obj);
            _queuesToProcess.Enqueue(key);
        }

        #endregion

        #region Helper Methods
        
        /// <summary>
        /// Gets the key.
        /// </summary>
        /// <param name="exchangeId">The exchange identifier.</param>
        /// <param name="queueId">The queue identifier.</param>
        /// <returns></returns> 
        private string GetKey(string exchangeId, string queueId)
        {
            return $"{exchangeId}/{queueId}";
        }

        #endregion

        #region Thread Methods

        /// <summary>
        /// Processes this instance.
        /// </summary>
        private void Process()
        {
            _logger.Debug("Starting evented queue handler processing thread");

            while (IsRunning)
            {
                if (_queuesToProcess.IsEmpty)
                { 
                    Thread.Sleep(20);
                    continue;
                }

                _queuesToProcess.TryDequeue(out string queueId);

                if (string.IsNullOrEmpty(queueId))
                    continue;

                if (_queues[queueId].CanDequeue)
                    _queues[queueId].Dequeue();
            }
        }

        #endregion
    }
}