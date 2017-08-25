using System;

namespace Loki.SignalServer.Common.Cache.InMemory
{
    public struct InMemoryCacheObject
    {
        public DateTime Timestamp { get; }
        public object Payload { get; }

        public InMemoryCacheObject(object payload)
        {
            Timestamp = DateTime.UtcNow;
            Payload = payload;
        }
    }
}