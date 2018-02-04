using System;
using System.Text;
using Loki.Interfaces.Dependency;
using Loki.Interfaces.Logging;
using Loki.SignalServer.Interfaces.Queues;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Loki.SignalServer.Common.Queues.RabbitMq
{
    public class RabbitEventedQueue<T> : IEventedQueue<T>
    {
        #region Properties

        /// <summary>
        /// Gets the count.
        /// </summary>
        /// <value>
        /// The count.
        /// </value>
        public int Count => throw new NotSupportedException();

        /// <summary>
        /// Gets a value indicating whether this instance can dequeue.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance can dequeue; otherwise, <c>false</c>.
        /// </value>
        public bool CanDequeue => false;
        
        #endregion

        #region Private Readonly Variables

        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The Json Serializer Settings
        /// </summary>
        private readonly JsonSerializerSettings _jss;

        /// <summary>
        /// The exchange identifier
        /// </summary>
        private readonly string _exchangeId;

        /// <summary>
        /// The queue identifier
        /// </summary>
        private readonly string _queueId;

        /// <summary>
        /// The routekey
        /// </summary>
        private readonly string _routekey;

        /// <summary>
        /// The channel
        /// </summary>
        private readonly IModel _channel;
        
        #endregion
        
        #region Events

        /// <summary>
        /// Occurs when [dequeued].
        /// </summary>
        public event EventHandler<T> Dequeued;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="RabbitEventedQueue{T}" /> class.
        /// </summary>
        public RabbitEventedQueue(string exchangeId, string queueId, string routeKey, IModel channel, IDependencyUtility dependencyUtility)
        {
            _exchangeId = exchangeId;
            _queueId = queueId;
            _routekey = routeKey;
            _channel = channel;

            _logger = dependencyUtility.Resolve<ILogger>();
            _jss = dependencyUtility.Resolve<JsonSerializerSettings>();

            AddConsumer();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Enqueues the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void Enqueue(T item)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item, _jss));

            _channel.BasicPublish(_exchangeId, _routekey, null, bytes);
        }

        /// <summary>
        /// Dequeues this instance.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public T Dequeue()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Adds the consumer.
        /// </summary>
        private void AddConsumer()
        {
            EventingBasicConsumer consumer = new EventingBasicConsumer(_channel);
            consumer.Received += ConsumerOnReceived;

            _channel.BasicConsume(_queueId, true, consumer);
        }

        /// <summary>
        /// The event which fires when the consumer receives a delivery
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="basicDeliverEventArgs">The <see cref="BasicDeliverEventArgs"/> instance containing the event data.</param>
        private void ConsumerOnReceived(object sender, BasicDeliverEventArgs basicDeliverEventArgs)
        {
            byte[] body = basicDeliverEventArgs.Body;

            T result = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(body), _jss);
            
            Dequeued?.Invoke(this, result);
        }

        #endregion
    }
}
