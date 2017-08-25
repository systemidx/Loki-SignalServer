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
        private readonly IDependencyUtility _dependencyUtility;
        private readonly ILogger _logger;

        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _cache;
        private readonly TimeSpan _expiry;

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

        public T Get<T>(string key) where T : class
        {
            string value = _cache.StringGet(key);

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

        public T[] Search<T>(Func<T, bool> predicate) where T : class
        {
            throw new NotImplementedException("Searching is not supported in this cache implementation. Please use in-memory caches for searching.");
        }

        public void Set<T>(string key, T value)
        {
            _cache.StringSet(key, JsonConvert.SerializeObject(value));
        }
    }
}
