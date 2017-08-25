using System;
using System.Linq;
using Loki.Interfaces.Dependency;
using Loki.Interfaces.Logging;
using Loki.SignalServer.Extensions.Interfaces;
using Loki.SignalServer.Interfaces.Exceptions;
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

        #region Private Variables

        private bool _isInitialized;

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
        /// Initializes this instance.
        /// </summary>
        public void Initialize()
        {
            if (_extensionLoader == null)
                _extensionLoader = _dependencyUtility.Resolve<IExtensionLoader>();

            if (_logger == null)
                _logger = _dependencyUtility.Resolve<ILogger>();

            _isInitialized = true;
        }

        /// <summary>
        /// Routes the specified signal.
        /// </summary>
        /// <param name="signal">The signal.</param>
        public ISignal Route(ISignal signal)
        {
            if (!_isInitialized)
                throw new DependencyNotInitException(nameof(SignalRouter));

            IExtension extension = GetExtension(signal);
            if (extension == null)
                throw new InvalidExtensionException($"Attempted to route an to an invalid extension. Route: {signal.Route}");

            _logger.Debug($"Request: {JsonConvert.SerializeObject(signal)}");

            ISignal response = extension.ExecuteAction(signal.Action, signal);

            if (response == null)
                return null;

            _logger.Debug($"Response: {JsonConvert.SerializeObject(response)}");

            return response;
        }

        /// <summary>
        /// Routes the specified signal from an extension to another extension.
        /// </summary>
        /// <param name="signal">The signal.</param>
        /// <returns></returns>
        /// <exception cref="DependencyNotInitException">SignalRouter</exception>
        public ISignal RouteExtension(ISignal signal)
        {
            if (!_isInitialized)
                throw new DependencyNotInitException(nameof(SignalRouter));

            IExtension extension = GetExtension(signal);
            if (extension == null)
                throw new InvalidExtensionException($"Attempted to route an to an invalid extension. Route: {signal.Route}");    

            _logger.Debug($"Cross Extension Request: {JsonConvert.SerializeObject(signal)}");

            ISignal response = extension.ExecuteCrossExtensionAction(signal.Action, signal);
            if (response == null)
                return null;

            _logger.Debug($"Cross Extension Response: {JsonConvert.SerializeObject(response)}");

            return response;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Gets the extension.
        /// </summary>
        /// <param name="signal">The signal.</param>
        /// <returns></returns>
        private IExtension GetExtension(ISignal signal)
        {
            if (signal == null)
            {
                _logger.Warn("Null packet attempted to route");
                return null;
            }

            if (!signal.IsValid)
            {
                _logger.Warn($"Invalid packet attempted to route:\r\n{JsonConvert.SerializeObject(signal)}");
                return null;
            }

            IExtension extension = _extensionLoader.Extensions.FirstOrDefault(x => string.Equals(signal.Extension, x.Name, StringComparison.InvariantCultureIgnoreCase));
            if (extension == null)
            {
                _logger.Warn($"Unable to route signal to extension: {signal.Extension}");
                return null;
            }

            return extension;
        }

        #endregion
    }
}