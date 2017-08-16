using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

        private readonly IDependencyUtility _dependencyUtility;
        private readonly ConcurrentQueue<string> _queuesToProcess = new ConcurrentQueue<string>();
        private readonly ConcurrentDictionary<string, IEventedQueue<T>> _queues = new ConcurrentDictionary<string, IEventedQueue<T>>();

        private readonly Dictionary<QueueService,Func<string, string, IEventedQueue<T>>> _queueGenerators = new Dictionary<QueueService, Func<string, string, IEventedQueue<T>>>();

        #endregion

        #region Vendor Specific Variables
        
        #endregion

        private QueueService _queueService;
        private Thread _processingThread;

        private IThreadHelper _threadHelper;
        private ILogger _logger;
        private IConfigurationHandler _config;

        public bool IsRunning { get; private set; }

        public EventedQueueHandler(IDependencyUtility dependencyUtility)
        {
            _dependencyUtility = dependencyUtility;

            CreateQueueGenerators();
        }

        public void Start()
        {
            IsRunning = true;
            
            _threadHelper = _dependencyUtility.Resolve<IThreadHelper>();
            _logger = _dependencyUtility.Resolve<ILogger>();
            _config = _dependencyUtility.Resolve<IConfigurationHandler>();

            _queueService = _config.Get<QueueService>("queue:service");
            _processingThread = _threadHelper.CreateAndRun(Process);
        }

        public void Stop()
        {
            IsRunning = false;
        }

        #region Public Methods

        public void CreateQueue(string exchangeId, string queueId)
        {
            string key = GetKey(exchangeId, queueId);

            if (_queues.ContainsKey(key))
                throw new ArgumentException(nameof(key));

            _queues[key] = _queueGenerators[this._queueService].Invoke(exchangeId, queueId);
        }

        public void RemoveQueue(string exchangeId, string queueId)
        {
            string key = GetKey(exchangeId, queueId);

            if (!_queues.ContainsKey(key))
                throw new ArgumentException(nameof(key));

            _queues.TryRemove(key, out _);
        }

        public void AddEvent(string exchangeId, string queueId, EventHandler<T> eventHandler)
        {
            string key = GetKey(exchangeId, queueId);

            if (!_queues.ContainsKey(key))
                throw new ArgumentException(nameof(key));

            _queues[key].Dequeued += eventHandler;
        }

        public void RemoveEvent(string exchangeId, string queueId, EventHandler<T> eventHandler)
        {
            string key = GetKey(exchangeId, queueId);

            if (!_queues.ContainsKey(key))
                throw new ArgumentException(nameof(key));

            _queues[key].Dequeued -= eventHandler;
        }

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

        private void CreateQueueGenerators()
        {
            _queueGenerators[QueueService.InMemory] = (exchangeId, queueId) => new InMemoryEventedQueue<T>();
            _queueGenerators[QueueService.RabbitMq] = (exchangeId, queueId) => new RabbitEventedQueue<T>(exchangeId, queueId, _dependencyUtility);
        }

        private string GetKey(string exchangeId, string queueId)
        {
            return $"{exchangeId}/{queueId}";
        }

        #endregion

        #region Thread Methods

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