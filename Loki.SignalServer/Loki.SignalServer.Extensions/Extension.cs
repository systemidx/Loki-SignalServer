using System;
using System.Collections.Concurrent;
using Loki.Interfaces.Dependency;
using Loki.Interfaces.Logging;
using Loki.Server.Logging;
using Loki.SignalServer.Extensions.Interfaces;
using Loki.SignalServer.Interfaces.Router;

namespace Loki.SignalServer.Extensions
{
    public abstract class Extension : IExtension
    {
        public readonly string ExtensionName;

        protected readonly IDependencyUtility DependencyUtility;
        protected readonly ILogger Logger;

        private readonly ConcurrentDictionary<string, Func<ISignal, ISignal>> _actions = new ConcurrentDictionary<string, Func<ISignal, ISignal>>();

        protected Extension(string extensionName, IDependencyUtility dependencyUtility)
        {
            ExtensionName = extensionName;

            DependencyUtility = dependencyUtility;
            Logger = DependencyUtility.Resolve<ILogger>() ?? DependencyUtility.Register(new Logger());
        }

        public void RegisterAction(string action, Func<ISignal, ISignal> signalAction)
        {
            _actions[action] = signalAction;

            Logger.Debug($"Registered extension action: {ExtensionName}/{action}");
        }

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
                Logger.Warn($"Attempted to execute: {ExtensionName}/{action}.\tCould not load action from extension cache.");
                return null;
            }

            return func.Invoke(signal);
        }
    }
}
