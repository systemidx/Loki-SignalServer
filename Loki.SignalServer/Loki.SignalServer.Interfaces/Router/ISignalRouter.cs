using System.Collections.Generic;

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

        /// <summary>
        /// Routes the specified signal to an extension.
        /// </summary>
        /// <param name="signal"></param>
        /// <returns></returns>
        ISignal RouteExtension(ISignal signal);

        /// <summary>
        /// Broadcasts the signal.
        /// </summary>
        /// <param name="entityId">The entity identifier.</param>
        /// <param name="signal">The signal.</param>
        void BroadcastSignal(string entityId, ISignal signal);

        /// <summary>
        /// Broadcasts a signal to a range of entities
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="signal"></param>
        void BroadcastSignal(IEnumerable<string> entities, ISignal signal);
    }
}
