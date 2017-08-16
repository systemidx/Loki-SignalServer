using System.Linq;
using Loki.Interfaces.Dependency;
using Loki.Interfaces.Logging;
using Loki.SignalServer.Extensions.Interfaces;
using Loki.SignalServer.Interfaces.Router;
using Newtonsoft.Json;

namespace Loki.SignalServer.Router
{
    public class SignalRouter : ISignalRouter
    {
        #region Readonly Variables

        /// <summary>
        /// The dependency utility
        /// </summary>
        private readonly IDependencyUtility _dependencyUtility;

        /// <summary>
        /// The extension loader
        /// </summary>
        private IExtensionLoader _extensionLoader;

        /// <summary>
        /// The logger
        /// </summary>
        private ILogger _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="SignalRouter"/> class.
        /// </summary>
        /// <param name="dependencyUtility">The dependency utility.</param>
        public SignalRouter(IDependencyUtility dependencyUtility)
        {
            _dependencyUtility = dependencyUtility;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Routes the specified signal.
        /// </summary>
        /// <param name="signal">The signal.</param>
        public void Route(ISignal signal)
        {
            if (_extensionLoader == null)
                _extensionLoader = _dependencyUtility.Resolve<IExtensionLoader>();

            if (_logger == null)
                _logger = _dependencyUtility.Resolve<ILogger>();

            if (signal == null)
            {
                _logger.Warn($"Null packet attempted to route");
                return;
            }

            if (!signal.IsValid)
            { 
                _logger.Warn($"Invalid packet attempted to route:\r\n{JsonConvert.SerializeObject(signal)}");
                return;
            }

            IExtension extension = _extensionLoader.Extensions.FirstOrDefault(x => signal.Extension == x.Name);
            if (extension == null)
            {
                _logger.Warn($"Unable to route signal to extension: {signal.Extension}");
                return;
            }

            extension.ExecuteAction(signal.Action, signal);
        }

        #endregion
    }
}