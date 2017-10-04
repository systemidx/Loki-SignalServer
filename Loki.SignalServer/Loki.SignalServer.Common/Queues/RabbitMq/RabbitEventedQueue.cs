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
        private string RoutingKey => Parameters["RouteKey"];

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
        public RabbitEventedQueue(IDependencyUtility dependencyUtility, IEventedQueueParameters parameters)
        {
            Parameters = parameters;

            _logger = dependencyUtility.Resolve<ILogger>();
            _jss = dependencyUtility.Resolve<JsonSerializerSettings>();

            string host = Parameters["Host"];
            string vHost = Parameters["VirtualHost"];
            string username = Parameters["Username"];
            string password = Parameters["Password"];
            string exchangeId = parameters["ExchangeId"];
            string exchangeType = parameters["ExchangeType"];

            bool exchangeDurable = parameters["ExchangeDurable"] ?? false;
            bool exchangeAutoDelete = parameters["ExchangeAutoDelete"] ?? false;

            string queueId = parameters["QueueId"];
            string routingKey = parameters["RouteKey"];

            bool queueDurable = parameters["Durable"];
            bool queueTransient = parameters["Transient"];
            bool queueAutoDelete = parameters["AutoDelete"];
            
            IConnectionFactory factory = new ConnectionFactory
            {
                UserName = username,
                Password = password,
                VirtualHost = vHost,
                Endpoint = new AmqpTcpEndpoint(host)
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
                _channel.ExchangeDeclare(exchangeId, exchangeType, exchangeDurable, exchangeAutoDelete);
                _channel.QueueDeclare(queueId, queueDurable, queueTransient, queueAutoDelete, null);
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
