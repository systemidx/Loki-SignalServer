namespace Loki.SignalServer.Interfaces.Cache
{
    public interface ICacheHandler
    {
        ICache GetCache(string key);
        void AddCache(string key, CacheService cacheType, int cacheExpiryInSec, string connectionString = null);
    }
}