using System;
using System.Collections.Generic;

namespace Loki.SignalServer.Interfaces.Cache
{
    public interface ICache
    {
        T Get<T>(string key) where T : class;
        T[] Search<T>(Func<T, bool> predicate) where T : class;
        void Set<T>(string key, T value);
    }
}