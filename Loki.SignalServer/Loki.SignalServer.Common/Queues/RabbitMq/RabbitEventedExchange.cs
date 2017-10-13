using System.Text;
using Loki.Interfaces.Dependency;
using Loki.Interfaces.Logging;
using Loki.SignalServer.Interfaces.Queues;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace Loki.SignalServer.Common.Queues.RabbitMq
{
    public class RabbitEventedExchange<T> : IEventedExchange<T>
    {
        #region Readonly Variables

        /// <summary>
        /// The channel
        /// </summary>
        private readonly IModel _channel;

        /// <summary>
        /// The channel lock
        /// </summary>
        private readonly object _channelLock = new object();

        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The JSS
        /// </summary>
        private readonly JsonSerializerSettings _jss;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public string Id { get; }

        /// <summary>
        /// Gets the exchange identifier.
        /// </summary>
        /// <value>
        /// The exchange identifier.
        /// </value>
        public string ExchangeId { get; }

        /// <summary>
        /// Gets the route key.
        /// </summary>
        /// <value>
        /// The route key.
        /// </value>
        public string RouteKey { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="RabbitEventedExchange{T}"/> class.
        /// </summary>
        /// <param name="exchangeId">The exchange identifier.</param>
        /// <param name="routeKey">The route key.</param>
        /// <param name="channel">The channel.</param>
        /// <param name="dependencyUtility">The dependency utility.</param>
        public RabbitEventedExchange(string exchangeId, string routeKey, IModel channel, IDependencyUtility dependencyUtility)
        {
            ExchangeId = exchangeId;
            RouteKey = routeKey;
            Id = exchangeId;

            _channel = channel;
            _jss = dependencyUtility.Resolve<JsonSerializerSettings>();
            _logger = dependencyUtility.Resolve<ILogger>();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Publishes the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void Publish(T item)
        {
            string serializedItem = JsonConvert.SerializeObject(item, _jss);

            byte[] bytes = Encoding.UTF8.GetBytes(serializedItem);

            _logger.Debug($"Publishing {bytes.Length} bytes to {ExchangeId}");

            lock (_channelLock)
                _channel.BasicPublish(ExchangeId, RouteKey, true, null, bytes);
        }

        #endregion
    }
}
