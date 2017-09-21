using System;
using System.Text;
using Loki.Interfaces.Dependency;
using Loki.Interfaces.Logging;
using Loki.Server.Logging;
using Loki.SignalServer.Interfaces.Configuration;
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
        public int Count
        {
            get {
                try
                {
                    uint val = 0;
                    lock (_channelLock)
                        val = _channel.MessageCount(QueueId);
                    return Convert.ToInt32(val);
                }
                catch (JsonSerializationException ex)
                {
                    _logger.Error(ex);
                    return 0;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance can dequeue.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance can dequeue; otherwise, <c>false</c>.
        /// </value>
        public bool CanDequeue => false;

        /// <summary>
        /// Gets or sets the parameters.
        /// </summary>
        /// <value>
        /// The parameters.
        /// </value>
        public IEventedQueueParameters Parameters { get; set; }


        /// <summary>
        /// Gets the queue identifier.
        /// </summary>
        /// <value>
        /// The queue identifier.
        /// </value>
        private string QueueId => Parameters["QueueId"];

        /// <summary>
        /// Gets the exchange identifier.
        /// </summary>
        /// <value>
        /// The exchange identifier.
        /// </value>
        private string ExchangeId => Parameters["ExchangeId"];

        /// <summary>
        /// Gets the routing key.
        /// </summary>
        /// <value>
        /// The routing key.
        /// </value>
        private string RoutingKey => Parameters["RoutingKey"];

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
        /// The parameters
        /// </summary>
        private readonly IEventedQueueParameters _parameters;

        #region RabbitMQ Variables

        /// <summary>
        /// The channel
        /// </summary>
        private readonly IModel _channel;

        /// <summary>
        /// The channel lock
        /// </summary>
        private readonly object _channelLock = new object();

        #endregion

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
        //public RabbitEventedQueue(string exchangeId, string queueId, IDependencyUtility dependencyUtility, string exchangeType = ExchangeType.Direct, string routeKey = "", bool durable = true, bool transient = false, bool autoDelete = false)
        public RabbitEventedQueue(IDependencyUtility dependencyUtility, IEventedQueueParameters parameters)
        {
            _parameters = parameters;
            _logger = dependencyUtility.Resolve<ILogger>();
            _jss = dependencyUtility.Resolve<JsonSerializerSettings>();

            string exchangeId = parameters["ExchangeId"];
            string exchangeType = parameters["ExchangeType"];
            string queueId = parameters["QueueId"];
            string routingKey = parameters["RouteKey"];

            bool durable = parameters["Durable"];
            bool transient = parameters["Transient"];
            bool autoDelete = parameters["AutoDelete"];

            IConfigurationHandler config = dependencyUtility.Resolve<IConfigurationHandler>();
            
            IConnectionFactory factory = new ConnectionFactory
            {
                UserName = config.Get("queue:username"),
                Password = config.Get("queue:password"),
                VirtualHost = config.Get("queue:vhost"),
                Endpoint = new AmqpTcpEndpoint(config.Get("queue:host"))
            };

            IConnection connection;
            try
            {
                connection = factory.CreateConnection();
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                throw;
            }
            finally
            {
                _logger.Debug("Connected to RabbitMQ service");
            }


            lock (_channelLock)
            { 
                _channel = connection.CreateModel();
                _channel.ExchangeDeclare(exchangeId, exchangeType);
                _channel.QueueDeclare(queueId, durable, transient, autoDelete, null);
                _channel.QueueBind(queueId, exchangeId, routingKey);

                EventingBasicConsumer consumer = new EventingBasicConsumer(_channel);
                consumer.Received += ConsumerOnReceived;

                _channel.BasicConsume(queueId, true, consumer);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Enqueues the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void Enqueue(T item)
        {
            string serializedItem = JsonConvert.SerializeObject(item, _jss);

            byte[] bytes = Encoding.UTF8.GetBytes(serializedItem);

            lock (_channelLock)
                _channel.BasicPublish(ExchangeId, RoutingKey, null, bytes);
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
