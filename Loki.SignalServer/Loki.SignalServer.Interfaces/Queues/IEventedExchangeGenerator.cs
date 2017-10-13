using Loki.SignalServer.Interfaces.Utility;

namespace Loki.SignalServer.Interfaces.Queues
{
    public interface IEventedExchangeGenerator
    {
        /// <summary>
        /// Adds the exchange.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="exchangeParameters">The exchange parameters.</param>
        void AddExchange<T>(IParameterList exchangeParameters);

        /// <summary>
        /// Publishes the specified exchange identifier.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="exchangeId">The exchange identifier.</param>
        /// <param name="item">The item.</param>
        void Publish<T>(string exchangeId, T item);
    }
}