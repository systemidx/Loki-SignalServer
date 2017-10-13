using System;

namespace Loki.SignalServer.Common.Cache.Redis
{
    public class CacheConnectionException : Exception
    {
        private readonly string _configuration;

        public override string Message => $"Failed to connect to cache server with configuration: {_configuration}";

        public CacheConnectionException(string configuration)
        {
            _configuration = configuration;
        }
    }
}