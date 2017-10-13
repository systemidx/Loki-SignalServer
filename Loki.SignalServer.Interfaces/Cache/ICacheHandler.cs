namespace Loki.SignalServer.Interfaces.Cache
{
    public interface ICacheHandler
    {
        /// <summary>
        /// Gets the cache.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        ICache GetCache(string key);

        /// <summary>
        /// Adds the cache.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="cacheType">Type of the cache.</param>
        /// <param name="cacheExpiryInSec">The cache expiry in sec.</param>
        /// <param name="connectionString">The connection string.</param>
        /// <returns></returns>
        ICache AddCache(string key, CacheService cacheType, int cacheExpiryInSec, string connectionString = null);
    }
}