using System.Threading;
using Loki.SignalServer.Common.Cache.InMemory;
using Loki.SignalServer.Interfaces.Cache;
using Xunit;

namespace Loki.SignalServer.UnitTests.Cache
{
    public class InMemoryCacheTests
    {
        [Fact]
        public void InMemoryCacheObjectReturnsPayload()
        {
            const string VALUE = "myvalue";

            ICache cache = new InMemoryCache(600);
            cache.Set("mykey", VALUE);

            Assert.Equal(VALUE, cache.Get<string>("mykey"));
        }

        [Fact]
        public void InMemoryCacheObjectReturnsNullWithInvalidKey()
        {
            ICache cache = new InMemoryCache(600);

            Assert.Equal(null, cache.Get<string>("mykey"));
        }

        [Fact]
        public void InMemoryCacheObjectReturnsNullWhenCacheObjectExpires()
        {
            const string VALUE = "myvalue";

            ICache cache = new InMemoryCache(0);
            cache.Set("mykey", VALUE);
            
            Assert.Equal(null, cache.Get<string>("mykey"));
        }
    }
}