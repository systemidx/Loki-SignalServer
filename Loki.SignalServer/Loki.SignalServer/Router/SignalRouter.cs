using System.Linq;
using Loki.Interfaces.Dependency;
using Loki.Interfaces.Logging;
using Loki.SignalServer.Extensions.Interfaces;
using Loki.SignalServer.Interfaces.Router;

namespace Loki.SignalServer.Router
{
    public class SignalRouter : ISignalRouter
    {
        #region Readonly Variables

        /// <summary>
        /// The extension loader
        /// </summary>
        private readonly IExtensionLoader _extensionLoader;

        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="SignalRouter"/> class.
        /// </summary>
        /// <param name="dependencyUtility">The dependency utility.</param>
        public SignalRouter(IDependencyUtility dependencyUtility)
        {
            _extensionLoader = dependencyUtility.Resolve<IExtensionLoader>();
            _logger = dependencyUtility.Resolve<ILogger>();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Routes the specified signal.
        /// </summary>
        /// <param name="signal">The signal.</param>
        public void Route(ISignal signal)
        {
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