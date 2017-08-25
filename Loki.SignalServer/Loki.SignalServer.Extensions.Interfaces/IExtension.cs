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
        /// Registers the cross extension action.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="signalAction">The signal action.</param>
        void RegisterCrossExtensionAction(string action, Func<ISignal, ISignal> signalAction);

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

        /// <summary>
        /// Executes the cross extension action.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="signal">The signal.</param>
        /// <returns></returns>
        ISignal ExecuteCrossExtensionAction(string action, ISignal signal);

        /// <summary>
        /// Creates the response.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="request">The request.</param>
        /// <param name="payload">The payload.</param>
        /// <returns></returns>
        ISignal CreateResponse<T>(ISignal request, T payload);

        /// <summary>
        /// Creates the cross extension request.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="route">The route.</param>
        /// <param name="sourceExtension">The source extension.</param>
        /// <param name="payload">The payload.</param>
        /// <returns></returns>
        ISignal CreateCrossExtensionRequest<T>(string route, string sourceExtension, T payload);

        /// <summary>
        /// Creates the request.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="signal">The signal.</param>
        /// <returns></returns>
        ISignal SendCrossExtensionRequest<T>(ISignal signal);
    }
}