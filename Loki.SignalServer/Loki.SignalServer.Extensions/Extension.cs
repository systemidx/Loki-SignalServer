using System;
using System.Collections.Concurrent;
using Loki.Interfaces.Connections;
using Loki.Interfaces.Dependency;
using Loki.Interfaces.Logging;
using Loki.Server.Logging;
using Loki.SignalServer.Extensions.Interfaces;
using Loki.SignalServer.Interfaces.Router;

namespace Loki.SignalServer.Extensions
{
    public abstract class Extension : IExtension
    {
        public string Name { get; }

        protected readonly IDependencyUtility DependencyUtility;
        protected readonly ILogger Logger;

        private readonly ConcurrentDictionary<string, Func<ISignal, ISignal>> _actions = new ConcurrentDictionary<string, Func<ISignal, ISignal>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="Extension"/> class.
        /// </summary>
        /// <param name="extensionName">Name of the extension.</param>
        /// <param name="dependencyUtility">The dependency utility.</param>
        protected Extension(string extensionName, IDependencyUtility dependencyUtility)
        {
            Name = extensionName;

            DependencyUtility = dependencyUtility;
            Logger = DependencyUtility.Resolve<ILogger>() ?? DependencyUtility.Register(new Logger());
        }
        
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

        /// <summary>
        /// Registers the action.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="signalAction">The signalAction.</param>
        public void RegisterAction(string action, Func<ISignal, ISignal> signalAction)
        {
            _actions[action] = signalAction;

            Logger.Debug($"Registered extension action: {Name}/{action}");
        }

        /// <summary>
        /// Executes the action.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="signal">The signal.</param>
        /// <returns></returns>
        public ISignal ExecuteAction(string action, ISignal signal)
        {
            Func<ISignal, ISignal> func = null;

            int i = 0;
            do
            {
                _actions.TryGetValue(action, out func);
            } while (func == null && i < 3);

            if (func == null)
            { 
                Logger.Warn($"Attempted to execute: {Name}/{action}.\tCould not load action from extension cache.");
                return null;
            }

            return func.Invoke(signal);
        }
    }
}
