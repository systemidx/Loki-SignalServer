using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Loki.SignalServer.Interfaces.Cache;

namespace Loki.SignalServer.Common.Cache.InMemory
{
    public class InMemoryCache : ICache
    {
        private readonly ConcurrentDictionary<string, InMemoryCacheObject> _cache = new ConcurrentDictionary<string, InMemoryCacheObject>();
        private readonly int _expiryInSeconds;

        public InMemoryCache(int expiryInSeconds)
        {
            _expiryInSeconds = expiryInSeconds;
        }

        public T Get<T>(string key) where T: class
        {
            if (!_cache.ContainsKey(key))
                return null;
            
            TimeSpan lifespan = DateTime.UtcNow - _cache[key].Timestamp;
            if (lifespan.TotalSeconds > _expiryInSeconds)
            {
                _cache.TryRemove(key, out _);
                return null;
            }
            
            return _cache[key].Payload as T;
        }

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

        public void Set<T>(string key, T value)
        {
            _cache[key] = new InMemoryCacheObject(value);
        }
    }
}