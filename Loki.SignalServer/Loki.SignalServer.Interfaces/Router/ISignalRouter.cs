namespace Loki.SignalServer.Interfaces.Router
{
    public interface ISignalRouter
    {
        void Route(ISignal signal);
    }
}
