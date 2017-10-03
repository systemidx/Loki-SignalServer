using System;
using System.Collections.Concurrent;
using System.Text;
using Loki.Interfaces.Connections;
using Loki.Interfaces.Dependency;
using Loki.Interfaces.Logging;
using Loki.SignalServer.Common.Router;
using Loki.SignalServer.Extensions.Interfaces;
using Loki.SignalServer.Interfaces.Configuration;
using Loki.SignalServer.Interfaces.Router;
using Newtonsoft.Json;

namespace Loki.SignalServer.Extensions
{
    public abstract class Extension : IExtension
    {
        #region Readonly Variables

        /// <summary>
        /// The dependency utility
        /// </summary>
        protected readonly IDependencyUtility DependencyUtility;

        /// <summary>
        /// The logger
        /// </summary>
        protected readonly ILogger Logger;

        /// <summary>
        /// The configuration
        /// </summary>
        protected readonly IConfigurationHandler Config;

        /// <summary>
        /// The router
        /// </summary>
        private readonly ISignalRouter _router;

        /// <summary>
        /// The actions
        /// </summary>
        private readonly ConcurrentDictionary<string, Func<ISignal, ISignal>> _actions = new ConcurrentDictionary<string, Func<ISignal, ISignal>>();

        /// <summary>
        /// The cross extension actions
        /// </summary>
        private readonly ConcurrentDictionary<string, Func<ISignal, ISignal>> _crossExtensionActions = new ConcurrentDictionary<string, Func<ISignal, ISignal>>();

        #endregion

        #region Properties

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is initialized.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is initialized; otherwise, <c>false</c>.
        /// </value>
        public bool IsInitialized { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="Extension"/> class.
        /// </summary>
        /// <param name="extensionName">Name of the extension.</param>
        /// <param name="dependencyUtility">The dependency utility.</param>
        protected Extension(string extensionName, IDependencyUtility dependencyUtility)
        {
            Name = extensionName;

            DependencyUtility = dependencyUtility;

            Logger = DependencyUtility.Resolve<ILogger>();
            Config = DependencyUtility.Resolve<IConfigurationHandler>();
            _router = DependencyUtility.Resolve<ISignalRouter>();
        }

        #endregion

        #region Abstract Methods
        
        /// <summary>
        /// Registers the connection.
        /// </summary>
        /// <param name="connection">The connection.</param>
        public abstract void RegisterConnection(IWebSocketConnection connection);

        /// <summary>
        /// Unregisters the connection.
        /// </summary>
        /// <param name="connection">The connection.</param>
        public abstract void UnregisterConnection(IWebSocketConnection connection);

        #endregion

        #region Public Methods

        public virtual void Initialize()
        {
            IsInitialized = true;
        }

        /// <summary>
        /// Registers the action.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="signalAction">The signalAction.</param>
        public void RegisterAction(string action, Func<ISignal, ISignal> signalAction)
        {
            _actions[action.ToLowerInvariant()] = signalAction;

            Logger.Debug($"Registered extension action: {Name}/{action}");
        }

        /// <summary>
        /// Registers the cross extension action.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="signalAction">The signal action.</param>
        public void RegisterCrossExtensionAction(string action, Func<ISignal, ISignal> signalAction)
        {
            _crossExtensionActions[action.ToLowerInvariant()] = signalAction;

            Logger.Debug($"Registered cross extension action: {Name}/{action}");
        }

        /// <summary>
        /// Executes the action.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="signal">The signal.</param>
        /// <returns></returns>
        public ISignal ExecuteAction(string action, ISignal signal)
        {
            return ExecuteAction(action, signal, _actions);
        }

        /// <summary>
        /// Creates the response.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="request">The request.</param>
        /// <param name="payload">The payload.</param>
        /// <param name="newActionName"></param>
        /// <returns></returns>
        public ISignal CreateResponse<T>(ISignal request, T payload, string newActionName = null)
        {
            string serializedPayload = JsonConvert.SerializeObject(payload);

            return new Signal
            {
                Route = newActionName == null ? request.Route : request.Extension + '/' + newActionName,
                Sender = "server",
                Recipient = request.Sender,
                Payload = Encoding.UTF8.GetBytes(serializedPayload)
            };
        }

        #region Cross Extenion Methods


        /// <summary>
        /// Executes the cross extension action.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="signal">The signal.</param>
        /// <returns></returns>
        public ISignal ExecuteCrossExtensionAction(string action, ISignal signal)
        {
            return ExecuteAction(action, signal, _crossExtensionActions);
        }

        /// <summary>
        /// Creates the cross extension request.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="route">The route.</param>
        /// <param name="sourceExtension">The source extension.</param>
        /// <param name="payload">The payload.</param>
        /// <returns></returns>
        public ISignal CreateCrossExtensionRequest<T>(string route, string sourceExtension, T payload)
        {
            return new Signal
            {
                Route = route,
                Sender = sourceExtension,
                Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload))
            };
        }

        /// <summary>
        /// Creates the request.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="signal">The signal.</param>
        /// <returns></returns>
        public ISignal SendCrossExtensionRequest<T>(ISignal signal)
        {
            return _router.RouteExtension(signal);
        }

        #endregion

        #endregion

        #region Private Methods

        /// <summary>
        /// Executes the action.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="signal">The signal.</param>
        /// <param name="cache">The cache.</param>
        /// <returns></returns>
        private ISignal ExecuteAction(string action, ISignal signal, ConcurrentDictionary<string, Func<ISignal, ISignal>> cache)
        {
            Func<ISignal, ISignal> func = null;

            int i = 0;
            do
            {
                cache?.TryGetValue(action.ToLowerInvariant(), out func);
                ++i;
            } while (func == null && i < 3);

            if (func == null)
            {
                Logger.Warn($"Attempted to execute: {Name}/{action}.\tCould not load action from {nameof(cache)}.");
                return null;
            }

            ISignal response = null;
            try
            {
                response = func.Invoke(signal);
            }
            catch (MissingMethodException ex)
            {
                Logger.Error(ex);
                Logger.Warn("Please make sure you're using the latest libraries in your extensions.");
            }

            return response;
        }

        #endregion
    }
}
