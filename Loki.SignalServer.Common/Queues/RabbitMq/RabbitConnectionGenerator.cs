using System;
using Loki.Interfaces.Dependency;
using Loki.Interfaces.Logging;
using Loki.SignalServer.Interfaces.Utility;
using RabbitMQ.Client;

namespace Loki.SignalServer.Common.Queues.RabbitMq
{
    public static class RabbitConnectionGenerator
    {
        /// <summary>
        /// Creates the connection.
        /// </summary>
        /// <param name="dependencyUtility">The dependency utility.</param>
        /// <param name="connectionParameters">The connection parameters.</param>
        /// <returns></returns>
        public static IConnection CreateConnection(IDependencyUtility dependencyUtility, IParameterList connectionParameters)
        {
            string host = connectionParameters["Host"];
            string virtualHost = connectionParameters["VirtualHost"];
            string username = connectionParameters["Username"];
            string password = connectionParameters["Password"];

            IConnectionFactory connectionFactory = new ConnectionFactory
            {
                UserName = username,
                Password = password,
                VirtualHost = virtualHost,
                Endpoint = new AmqpTcpEndpoint(host)
            };

            ILogger logger = dependencyUtility.Resolve<ILogger>();
            IConnection connection;
            try
            {
                connection = connectionFactory.CreateConnection();
                logger.Debug($"Connected to RabbitMQ service: {host}{virtualHost}");
            }
            catch (Exception exception)
            {
                logger.Error(exception);
                throw;
            }

            return connection;
        }
    }
}