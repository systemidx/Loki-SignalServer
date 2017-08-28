namespace Loki.SignalServer.Interfaces.Router
{
    public interface ISignalRouter
    {
        /// <summary>
        /// Initializes this instance.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Routes the specified signal.
        /// </summary>
        /// <param name="signal">The signal.</param>
        void Route(ISignal signal);

        ISignal RouteExtension(ISignal signal);
    }
}
