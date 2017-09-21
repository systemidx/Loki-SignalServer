using System;
using System.Collections.Concurrent;
using System.Threading;
using Loki.Interfaces.Dependency;
using Loki.Interfaces.Logging;
using Loki.Interfaces.Threading;
using Loki.SignalServer.Common.Enum;
using Loki.SignalServer.Common.Queues.InMemory;
using Loki.SignalServer.Common.Queues.RabbitMq;
using Loki.SignalServer.Interfaces.Configuration;
using Loki.SignalServer.Interfaces.Queues;

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
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">CreateQueue</exception>
        /// <exception cref="T:System.ArgumentException">key</exception>
        public IEventedQueue<T> CreateQueue(IEventedQueueParameters parameters)
        {
            string key = GetKey(parameters);

            if (_queues.ContainsKey(key))
                return _queues[key];

            switch (_queueService)
            {
                case QueueService.InMemory:
                    _queues[key] = new InMemoryEventedQueue<T>();
                    break;

                case QueueService.RabbitMq:
                    _queues[key] = new RabbitEventedQueue<T>(_dependencyUtility, parameters);
                    break;

                default:
                    throw new InvalidOperationException(nameof(CreateQueue));
            }

            return _queues[key];
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
                return;

            _queues[key].Dequeued -= eventHandler;
        }

        /// <summary>
        /// Enqueues the specified queue identifier.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <param name="obj">The object.</param>
        public void Enqueue(IEventedQueueParameters parameters, T obj)
        {
            string key = GetKey(parameters);

            if (!_queues.ContainsKey(key))
                CreateQueue(parameters);

            _queues[key].Enqueue(obj);
            _queuesToProcess.Enqueue(key);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets the key.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        private string GetKey(IEventedQueueParameters parameters)
        {
            string exchangeId = parameters["ExchangeId"];
            string queueId = parameters["QueueId"];

            return GetKey(exchangeId, queueId);
        }

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