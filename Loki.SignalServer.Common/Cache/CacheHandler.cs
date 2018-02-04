using System.Collections.Concurrent;
using Loki.Interfaces.Dependency;
using Loki.SignalServer.Common.Cache.InMemory;
using Loki.SignalServer.Common.Cache.Redis;
using Loki.SignalServer.Interfaces.Cache;

namespace Loki.SignalServer.Common.Cache
{
    public class CacheHandler : ICacheHandler
    {
        #region Private Readonly Variables

        /// <summary>
        /// The dependency utility
        /// </summary>
        private readonly IDependencyUtility _dependencyUtility;

        /// <summary>
        /// The caches
        /// </summary>
        private readonly ConcurrentDictionary<string, ICache> _caches = new ConcurrentDictionary<string, ICache>();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheHandler"/> class.
        /// </summary>
        /// <param name="dependencyUtility">The dependency utility.</param>
        public CacheHandler(IDependencyUtility dependencyUtility)
        {
            _dependencyUtility = dependencyUtility;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the cache.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public ICache GetCache(string key)
        {
            return _caches.ContainsKey(key) ? _caches[key] : null;
        }

        /// <summary>
        /// Adds the cache.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="cacheType">Type of the cache.</param>
        /// <param name="cacheExpiryInSec">The cache expiry in sec.</param>
        /// <param name="connectionString">The connection string.</param>
        /// <returns></returns>
        public ICache AddCache(string key, CacheService cacheType, int cacheExpiryInSec, string connectionString = null)
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

            return cache;
        }

        #endregion
    }
}