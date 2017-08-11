using System;
using Loki.Interfaces.Connections;
using Loki.SignalServer.Interfaces.Router;

namespace Loki.SignalServer.Extensions.Interfaces
{
    public interface IExtension
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        string Name { get; }

        /// <summary>
        /// Registers the action.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="signalAction">The signalAction.</param>
        void RegisterAction(string action, Func<ISignal, ISignal> signalAction);

        /// <summary>
        /// Registers the connection.
        /// </summary>
        /// <param name="connection">The connection.</param>
        void RegisterConnection(IWebSocketConnection connection);

        /// <summary>
        /// Unregisters the connection.
        /// </summary>
        /// <param name="connection">The connection.</param>
        void UnregisterConnection(IWebSocketConnection connection);

        /// <summary>
        /// Executes the action.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="signal">The signal.</param>
        /// <returns></returns>
        ISignal ExecuteAction(string action, ISignal signal);
    }
}