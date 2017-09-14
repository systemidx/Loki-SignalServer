using System.Collections.Concurrent;
using Loki.Interfaces.Dependency;
using Loki.SignalServer.Common.Cache.InMemory;
using Loki.SignalServer.Common.Cache.Redis;
using Loki.SignalServer.Common.Enum;
using Loki.SignalServer.Interfaces.Cache;

namespace Loki.SignalServer.Common.Cache
{
    public class CacheHandler : ICacheHandler
    {
        private readonly IDependencyUtility _dependencyUtility;
        private readonly ConcurrentDictionary<string, ICache> _caches = new ConcurrentDictionary<string, ICache>();

        public CacheHandler(IDependencyUtility dependencyUtility)
        {
            _dependencyUtility = dependencyUtility;
        }

        public ICache GetCache(string key)
        {
            if (_caches.ContainsKey(key))
                return _caches[key];

            return null;
        }

        public void AddCache(string key, CacheService cacheType, int cacheExpiryInSec, string connectionString = null)
        {
            ICache cache;
            switch (cacheType)
            {
                case CacheService.Redis:
                    cache = new RedisCache(connectionString, cacheExpiryInSec, _dependencyUtility);
                    break;

                case CacheService.InMemory:
                default:
                    cache = new InMemoryCache(cacheExpiryInSec);
                    break;
            }

            _caches[key] = cache;
        }
    }
}