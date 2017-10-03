using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Loki.SignalServer.Interfaces.Cache;

namespace Loki.SignalServer.Common.Cache.InMemory
{
    public class InMemoryCache : ICache
    {
        #region Readonly Variables

        /// <summary>
        /// The cache
        /// </summary>
        private readonly ConcurrentDictionary<string, InMemoryCacheObject> _cache = new ConcurrentDictionary<string, InMemoryCacheObject>();

        /// <summary>
        /// The expiry in seconds
        /// </summary>
        private readonly int _expiryInSeconds;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryCache"/> class.
        /// </summary>
        /// <param name="expiryInSeconds">The expiry in seconds.</param>
        public InMemoryCache(int expiryInSeconds)
        {
            _expiryInSeconds = expiryInSeconds;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the specified key.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public T Get<T>(string key) where T: class
        {
            if (!_cache.ContainsKey(key))
                return null;
            
            if (_expiryInSeconds > -1)
            { 
                TimeSpan lifespan = DateTime.UtcNow - _cache[key].Timestamp;
                if (lifespan.TotalSeconds > _expiryInSeconds)
                {
                    _cache.TryRemove(key, out _);
                    return null;
                }
            }

            return _cache[key].Payload as T;
        }

        /// <summary>
        /// Searches the cache.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="predicate">The predicate.</param>
        /// <returns></returns>
        public T[] Search<T>(Func<T, bool> predicate) where T: class
        {
            string[] keys = _cache.Where(x => predicate(x.Value.Payload as T)).Select(x => x.Key).ToArray();
            
            if (!keys.Any())
                return null;

            List<T> values = new List<T>();
            foreach (string key in keys)
            {
                T cachedItem = Get<T>(key);
                if (cachedItem == null)
                    continue;

                values.Add(cachedItem);
            }

            return values.ToArray();
        }

        /// <summary>
        /// Sets the specified key.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void Set<T>(string key, T value)
        {
            _cache[key] = new InMemoryCacheObject(value);
        }

        #endregion

    }
}