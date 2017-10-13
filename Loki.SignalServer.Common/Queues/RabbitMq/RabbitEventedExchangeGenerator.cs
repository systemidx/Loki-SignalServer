using Loki.Interfaces.Dependency;
using Loki.Interfaces.Logging;
using Loki.SignalServer.Interfaces.Cache;
using Loki.SignalServer.Interfaces.Queues;
using Loki.SignalServer.Interfaces.Utility;
using RabbitMQ.Client;

namespace Loki.SignalServer.Common.Queues.RabbitMq
{
    public class RabbitEventedExchangeGenerator : IEventedExchangeGenerator
    {
        #region Readonly Variables

        /// <summary>
        /// The dependency utility
        /// </summary>
        private readonly IDependencyUtility _dependencyUtility;

        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The channel cache
        /// </summary>
        private readonly ICache _exchangeCache;
        
        /// <summary>
        /// The connection
        /// </summary>
        private readonly IConnection _connection;

        /// <summary>
        /// The connection lock
        /// </summary>
        private readonly object _connectionLock = new object();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="RabbitEventedExchangeGenerator" /> class.
        /// </summary>
        /// <param name="dependencyUtility">The dependency utility.</param>
        /// <param name="connectionParameters">The connection parameters.</param>
        /// <param name="connection">The connection.</param>
        public RabbitEventedExchangeGenerator(IDependencyUtility dependencyUtility, IParameterList connectionParameters, IConnection connection = null)
        {
            _dependencyUtility = dependencyUtility;
            _logger = _dependencyUtility.Resolve<ILogger>();
            _exchangeCache = _dependencyUtility.Resolve<ICacheHandler>().AddCache("rabbitmq-exchanges", CacheService.InMemory, -1);
            _connection = connection ?? RabbitConnectionGenerator.CreateConnection(_dependencyUtility, connectionParameters);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds the exchange.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="exchangeParameters">The exchange parameters.</param>
        public void AddExchange<T>(IParameterList exchangeParameters)
        {
            string exchangeId = exchangeParameters["ExchangeId"];
            string exchangeType = exchangeParameters["ExchangeType"];
            string routeKey = exchangeParameters["RouteKey"];

            bool durable = exchangeParameters["ExchangeDurable"] ?? false;
            bool autoDelete = exchangeParameters["ExchangeAutoDelete"] ?? false;

            IModel channel = null;

            lock (_connectionLock)
                channel = _connection.CreateModel();

            _logger.Debug($"Declaring RabbitMQ exchange: {exchangeId}");

            channel.ExchangeDeclare(exchangeId, exchangeType, durable, autoDelete);

            IEventedExchange<T> exchange = new RabbitEventedExchange<T>(exchangeId, routeKey, channel, _dependencyUtility);
            _exchangeCache.Set(exchangeId, exchange);
        }

        /// <summary>
        /// Publishes the specified exchange identifier.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="exchangeId">The exchange identifier.</param>
        /// <param name="item">The item.</param>
        public void Publish<T>(string exchangeId, T item)
        {
            IEventedExchange<T> exchange = _exchangeCache.Get<IEventedExchange<T>>(exchangeId);
            exchange?.Publish(item);
        }

        #endregion
    }
}
