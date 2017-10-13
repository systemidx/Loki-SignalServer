using System;
using Loki.Interfaces.Dependency;
using Loki.Interfaces.Logging;
using Loki.SignalServer.Interfaces.Cache;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Loki.SignalServer.Common.Cache.Redis
{
    public class RedisCache : ICache
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
        /// The redis connection multiplexer
        /// </summary>
        private readonly IConnectionMultiplexer _redis;

        /// <summary>
        /// The cache database
        /// </summary>
        private readonly IDatabase _cache;

        /// <summary>
        /// The expiry
        /// </summary>
        private readonly TimeSpan _expiry;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisCache"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="expiryInSeconds">The expiry in seconds.</param>
        /// <param name="dependencyUtility">The dependency utility.</param>
        /// <exception cref="CacheConnectionException"></exception>
        public RedisCache(string connectionString, int expiryInSeconds, IDependencyUtility dependencyUtility)
        {
            _dependencyUtility = dependencyUtility;
            _logger = _dependencyUtility.Resolve<ILogger>();

            _redis = ConnectionMultiplexer.Connect(connectionString);
            _cache = _redis.GetDatabase();
            _expiry = new TimeSpan(0, 0, expiryInSeconds);

            if (_redis.IsConnected)
                _logger.Debug("Established Redis connection");
            else
                throw new CacheConnectionException(connectionString);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the specified key.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public T Get<T>(string key) where T : class
        {
            string value = null;

            try
            {
                value = _cache.StringGet(key);
            }
            catch (RedisTimeoutException ex)
            {
                _logger.Error(ex);
            }

            if (value == null)
                return null;

            try
            {
                return JsonConvert.DeserializeObject<T>(value);
            }
            catch (JsonSerializationException ex)
            {
                _logger.Error(ex);
            }

            return null;
        }

        /// <summary>
        /// Searches the cache.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="predicate">The predicate.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException">Searching is not supported in this cache implementation. Please use in-memory caches for searching.</exception>
        public T[] Search<T>(Func<T, bool> predicate) where T : class
        {
            throw new NotImplementedException("Searching is not supported in this cache implementation. Please use in-memory caches for searching.");
        }

        /// <summary>
        /// Sets the specified key.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void Set<T>(string key, T value)
        {
            try
            {
                _cache.StringSet(key, JsonConvert.SerializeObject(value));
                _cache.KeyExpire(key, _expiry);
            }
            catch (RedisTimeoutException ex)
            {
                _logger.Error(ex);
            }
        }

        #endregion
    }
}
