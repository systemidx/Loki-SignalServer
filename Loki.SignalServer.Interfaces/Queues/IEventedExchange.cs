namespace Loki.SignalServer.Interfaces.Queues
{
    public interface IEventedExchange<in T>
    {
        string Id { get; }
        string ExchangeId { get; }
        string RouteKey { get; }

        void Publish(T item);
    }
}