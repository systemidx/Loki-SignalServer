using System;
using System.Collections.Generic;
using System.Linq;
using Loki.Interfaces.Connections;
using Loki.Interfaces.Dependency;
using Loki.Interfaces.Logging;
using Loki.SignalServer.Common.Queues;
using Loki.SignalServer.Common.Router;
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
        
        private const string CONFIGURATION_KEY_EXCHANGE_INCOMING = "router:exchange:requests";
        private const string CONFIGURATION_KEY_EXCHANGE_OUTGOING = "router:exchange:responses";
        private const string CONFIGURATION_KEY_QUEUE_INCOMING = "router:queue:queue-names:requests";
        private const string CONFIGURATION_KEY_QUEUE_OUTGOING = "router:queue:queue-names:responses";
        private const string CONFIGURATION_KEY_QUEUE_HOST = "router:queue:host";
        private const string CONFIGURATION_KEY_QUEUE_VHOST = "router:queue:vhost";
        private const string CONFIGURATION_KEY_QUEUE_USERNAME = "router:queue:username";
        private const string CONFIGURATION_KEY_QUEUE_PASSWORD = "router:queue:password";

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
        /// The request queue
        /// </summary>
        private IEventedQueue<ISignal> _requestQueue;

        /// <summary>
        /// The response queue
        /// </summary>
        private IEventedQueue<ISignal> _responseQueue;
        
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

            _queueHandler.Start();

            CreateRequestQueue();
            CreateResponseQueue();

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

            _requestQueue.Enqueue(signal);
        }

        /// <inheritdoc />
        /// <summary>
        /// Routes the specified signal from an extension to another extension.
        /// </summary>
        /// <param name="signal">The signal.</param>
        /// <returns></returns>
        /// <exception cref="T:Loki.SignalServer.Interfaces.Exceptions.DependencyNotInitException">SignalRouter</exception>
        public ISignal RouteExtension(ISignal signal)
        {
            if (!_isInitialized)
                throw new DependencyNotInitException(nameof(SignalRouter));

            IExtension extension = GetExtension(signal);
            if (extension == null)
                throw new InvalidExtensionException($"Attempted to route an to an invalid extension. Route: {signal.Route}");    

            _logger.Debug(signal.Format("Cross Request"));
            
            ISignal response = extension.ExecuteCrossExtensionAction(signal.Action, signal);
            if (response == null)
                return null;

            _logger.Debug(signal.Format("Cross Response"));

            return response;
        }

        /// <inheritdoc />
        /// <summary>
        /// Broadcasts the signal.
        /// </summary>
        /// <param name="entityId">The entity identifier.</param>
        /// <param name="signal">The signal.</param>
        /// <exception cref="T:Loki.SignalServer.Interfaces.Exceptions.DependencyNotInitException">SignalRouter</exception>
        public void BroadcastSignal(string entityId, ISignal signal)
        {
            if (!_isInitialized)
                throw new DependencyNotInitException(nameof(SignalRouter));

            _logger.Debug(signal.Format("Direct Response"));

            signal.Sender = "server";

            //If on server, send signal
            IWebSocketConnection[] connections = _connectionManager.GetConnectionsByClientIdentifier(entityId);

            if (connections == null || connections.Length == 0)
                return;

            signal.Recipient = connections[0].ClientIdentifier;
            foreach (IWebSocketConnection connection in connections)
                connection?.SendText(signal);
        }

        /// <inheritdoc />
        /// <summary>
        /// Broadcasts the signal.
        /// </summary>
        /// <param name="entities">The entities.</param>
        /// <param name="signal">The signal.</param>
        /// <exception cref="T:Loki.SignalServer.Interfaces.Exceptions.DependencyNotInitException">SignalRouter</exception>
        public void BroadcastSignal(IEnumerable<string> entities, ISignal signal)
        {
            if (!_isInitialized)
                throw new DependencyNotInitException(nameof(SignalRouter));

            _logger.Debug(signal.Format("Direct Response"));

            signal.Sender = "server";
            
            foreach (string entityId in entities)
                BroadcastSignal(entityId, signal);
        }

        #endregion

        #region Private Methods


        /// <summary>
        /// Creates the request queue.
        /// </summary>
        private void CreateRequestQueue()
        {
            IEventedQueueParameters requestQueueParameters = new EventedQueueParameters();
            requestQueueParameters["Host"] = _config.Get(CONFIGURATION_KEY_QUEUE_HOST);
            requestQueueParameters["VirtualHost"] = _config.Get(CONFIGURATION_KEY_QUEUE_VHOST);
            requestQueueParameters["Username"] = _config.Get(CONFIGURATION_KEY_QUEUE_USERNAME);
            requestQueueParameters["Password"] = _config.Get(CONFIGURATION_KEY_QUEUE_PASSWORD);
            requestQueueParameters["ExchangeId"] = _config.Get(CONFIGURATION_KEY_EXCHANGE_INCOMING);
            requestQueueParameters["ExchangeType"] = ExchangeType.Fanout;
            requestQueueParameters["QueueId"] = _config.Get(CONFIGURATION_KEY_QUEUE_INCOMING);
            requestQueueParameters["RouteKey"] = "requestRouteKey";
            requestQueueParameters["Durable"] = true;
            requestQueueParameters["Transient"] = false;
            requestQueueParameters["AutoDelete"] = true;

            _requestQueue = _queueHandler.CreateQueue(requestQueueParameters);
            _requestQueue.Dequeued += HandleRequest;
        }

        /// <summary>
        /// Creates the response queue.
        /// </summary>
        private void CreateResponseQueue()
        {
            IEventedQueueParameters responseQueueParameters = new EventedQueueParameters();
            responseQueueParameters["Host"] = _config.Get(CONFIGURATION_KEY_QUEUE_HOST);
            responseQueueParameters["VirtualHost"] = _config.Get(CONFIGURATION_KEY_QUEUE_VHOST);
            responseQueueParameters["Username"] = _config.Get(CONFIGURATION_KEY_QUEUE_USERNAME);
            responseQueueParameters["Password"] = _config.Get(CONFIGURATION_KEY_QUEUE_PASSWORD);
            responseQueueParameters["ExchangeId"] = _config.Get(CONFIGURATION_KEY_EXCHANGE_OUTGOING);
            responseQueueParameters["ExchangeType"] = ExchangeType.Fanout;
            responseQueueParameters["QueueId"] = _config.Get(CONFIGURATION_KEY_QUEUE_OUTGOING);
            responseQueueParameters["RouteKey"] = "responseRouteKey";
            responseQueueParameters["Durable"] = true;
            responseQueueParameters["Transient"] = false;
            responseQueueParameters["AutoDelete"] = true;

            _responseQueue = _queueHandler.CreateQueue(responseQueueParameters);
            _responseQueue.Dequeued += HandleResponse;
        }

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

            if (!extension.IsInitialized)
                throw new ExtensionNotInitializedException(nameof(extension));

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

            _logger.Debug(signal.Format("Response"));

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

            IWebSocketConnection[] connections = _connectionManager.GetConnectionsByClientIdentifier(signal.Sender);
            if (!connections.Any())
                return;

            _logger.Debug(signal.Format("Request"));

            ISignal response = extension.ExecuteAction(signal.Action, signal);

            if (response == null)
                return;
            
            _responseQueue.Enqueue(response);
        }
        

        #endregion
    }
}