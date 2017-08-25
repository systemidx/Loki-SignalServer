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
                        val = _channel.MessageCount(_queueId);
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

        /// <summary>
        /// The exchange identifier
        /// </summary>
        private readonly string _exchangeId;

        /// <summary>
        /// The queue identifier
        /// </summary>
        private readonly string _queueId;

        /// <summary>
        /// The routing key
        /// </summary>
        private readonly string _routingKey = Guid.NewGuid().ToString();

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
        /// Initializes a new instance of the <see cref="RabbitEventedQueue{T}"/> class.
        /// </summary>
        /// <param name="exchangeId">The exchange identifier.</param>
        /// <param name="queueId">The queue identifier.</param>
        /// <param name="dependencyUtility">The dependency utility.</param>
        public RabbitEventedQueue(string exchangeId, string queueId, IDependencyUtility dependencyUtility)
        {
            _logger = dependencyUtility.Resolve<ILogger>();
            _jss = dependencyUtility.Resolve<JsonSerializerSettings>();
            _exchangeId = exchangeId;
            _queueId = queueId;

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
                _channel.ExchangeDeclare(exchangeId, ExchangeType.Direct);
                _channel.QueueDeclare(queueId, false, false, false, null);
                _channel.QueueBind(queueId, exchangeId, _routingKey);

                EventingBasicConsumer consumer = new EventingBasicConsumer(_channel);
                consumer.Received += ConsumerOnReceived;

                _channel.BasicConsume(_queueId, false, consumer);
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
                _channel.BasicPublish(_exchangeId, _routingKey, null, bytes);
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

            if (result != null)
                lock (_channelLock)
                    _channel.BasicAck(basicDeliverEventArgs.DeliveryTag, false);

            Dequeued?.Invoke(this, result);
        }

        #endregion
    }
}
