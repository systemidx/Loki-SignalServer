namespace Loki.SignalServer.Interfaces.Router
{
    public interface ISignalRouter
    {
        void Initialize();

        ISignal Route(ISignal signal);
        ISignal RouteExtension(ISignal signal);
    }
}
