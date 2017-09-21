namespace Loki.SignalServer.Interfaces.Queues
{
    public interface IEventedQueueParameters
    {
        dynamic this[string index] { get; set; }
    }
}