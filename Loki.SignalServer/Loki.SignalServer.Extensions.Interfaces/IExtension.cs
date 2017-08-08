using System;
using Loki.SignalServer.Interfaces.Router;

namespace Loki.SignalServer.Extensions.Interfaces
{
    public interface IExtension
    {
        /// <summary>
        /// Registers the action.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="signalAction">The signalAction.</param>
        void RegisterAction(string action, Func<ISignal, ISignal> signalAction);

        /// <summary>
        /// Executes the action.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="signal">The signal.</param>
        /// <returns></returns>
        ISignal ExecuteAction(string action, ISignal signal);
    }
}