using System;
using System.Linq;
using Loki.Interfaces.Connections;
using Loki.Interfaces.Dependency;
using Loki.Interfaces.Logging;
using Loki.SignalServer.Common.Queues;
using Loki.SignalServer.Extensions.Interfaces;
using Loki.SignalServer.Interfaces.Configuration;
using Loki.SignalServer.Interfaces.Exceptions;
using Loki.SignalServer.Interfaces.Queues;
using Loki.SignalServer.Interfaces.Router;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace Loki.SignalServer.Router
{
    public class SignalRouter : ISignalRouter
    {
        #region Constants
        
        private const string CONFIGURATION_KEY_EXCHANGE_INCOMING = "cluster:exchange:requests";
        private const string CONFIGURATION_KEY_EXCHANGE_OUTGOING = "cluster:exchange:responses";
        private const string CONFIGURATION_KEY_QUEUE_INCOMING = "cluster:queue:requests";
        private const string CONFIGURATION_KEY_QUEUE_OUTGOING = "cluster:queue:responses";

        #endregion

        #region Readonly Variables

        /// <summary>
        /// The dependency utility
        /// </summary>
        private readonly IDependencyUtility _dependencyUtility;
        
        #endregion

        #region Private Variables


        /// <summary>
        /// The extension loader
        /// </summary>
        private IExtensionLoader _extensionLoader;

        /// <summary>
        /// The logger
        /// </summary>
        private ILogger _logger;

        /// <summary>
        /// The queue handler
        /// </summary>
        private IEventedQueueHandler<ISignal> _queueHandler;

        /// <summary>
        /// The configuration
        /// </summary>
        private IConfigurationHandler _config;

        /// <summary>
        /// The connection manager
        /// </summary>
        private IWebSocketConnectionManager _connectionManager;

        /// <summary>
        /// The incoming signal exchange identifier
        /// </summary>
        private string _incomingSignalExchangeId;

        /// <summary>
        /// The incoming signal queue identifier
        /// </summary>
        private string _incomingSignalQueueId;

        /// <summary>
        /// The outgoing signal exchange identifier
        /// </summary>
        private string _outgoingSignalExchangeId;

        /// <summary>
        /// The outgoing signal queue identifier
        /// </summary>
        private string _outgoingSignalQueueId;

        /// <summary>
        /// Initialization flag
        /// </summary>
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

            if (_config == null)
                _config = _dependencyUtility.Resolve<IConfigurationHandler>();

            if (_connectionManager == null)
                _connectionManager = _dependencyUtility.Resolve<IWebSocketConnectionManager>();

            if (_queueHandler == null)
                _queueHandler = new EventedQueueHandler<ISignal>(_dependencyUtility);
            
            _incomingSignalExchangeId = _config.Get(CONFIGURATION_KEY_EXCHANGE_INCOMING);
            _outgoingSignalExchangeId = _config.Get(CONFIGURATION_KEY_EXCHANGE_OUTGOING);
            _incomingSignalQueueId = _config.Get(CONFIGURATION_KEY_QUEUE_INCOMING);
            _outgoingSignalQueueId = _config.Get(CONFIGURATION_KEY_QUEUE_OUTGOING);

            _queueHandler.Start();

            _queueHandler.CreateQueue(_incomingSignalExchangeId, _incomingSignalQueueId, ExchangeType.Fanout);
            _queueHandler.CreateQueue(_outgoingSignalExchangeId, _outgoingSignalQueueId, ExchangeType.Fanout);

            _queueHandler.AddEvent(_incomingSignalExchangeId, _incomingSignalQueueId, HandleRequest);
            _queueHandler.AddEvent(_outgoingSignalExchangeId, _outgoingSignalQueueId, HandleResponse);

            _isInitialized = true;
        }


        /// <summary>
        /// Routes the specified signal.
        /// </summary>
        /// <param name="signal">The signal.</param>
        public void Route(ISignal signal)
        {
            if (!_isInitialized)
                throw new DependencyNotInitException(nameof(SignalRouter));

            _queueHandler.Enqueue(_incomingSignalExchangeId, _incomingSignalQueueId, signal);
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


        /// <summary>
        /// Handles the response.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="signal">The signal.</param>
        private void HandleResponse(object sender, ISignal signal)
        {
            IWebSocketConnection[] connections = _connectionManager.GetConnectionsByClientIdentifier(signal.Recipient);

            if (!connections.Any())
                return;

            foreach (IWebSocketConnection connection in connections)
                connection.SendText(signal);
        }

        /// <summary>
        /// Handles the request.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="signal">The signal.</param>
        /// <exception cref="InvalidExtensionException"></exception>
        private void HandleRequest(object sender, ISignal signal)
        {
            IExtension extension = GetExtension(signal);
            if (extension == null)
                throw new InvalidExtensionException($"Attempted to route an to an invalid extension. Route: {signal.Route}");

            _logger.Debug($"Request: {JsonConvert.SerializeObject(signal)}");

            ISignal response = extension.ExecuteAction(signal.Action, signal);

            if (response == null)
                return;

            _logger.Debug($"Response: {JsonConvert.SerializeObject(response)}");

            _queueHandler.Enqueue(_outgoingSignalExchangeId, _outgoingSignalQueueId, response);
        }

        #endregion
    }
}