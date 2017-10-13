using System;
using System.Collections.Generic;
using System.Linq;
using Loki.Interfaces.Connections;
using Loki.Interfaces.Dependency;
using Loki.Interfaces.Logging;
using Loki.SignalServer.Common.Queues.RabbitMq;
using Loki.SignalServer.Common.Router;
using Loki.SignalServer.Common.Utility;
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

        #region Router Exchanges

        private const string CONFIGURATION_KEY_ROUTER_EXCHANGE_HOST = "router:exchanges:host:host";
        private const string CONFIGURATION_KEY_ROUTER_EXCHANGE_USERNAME = "router:exchanges:host:username";
        private const string CONFIGURATION_KEY_ROUTER_EXCHANGE_PASSWORD = "router:exchanges:host:password";
        private const string CONFIGURATION_KEY_ROUTER_EXCHANGE_VHOST = "router:exchanges:host:vhost";
        
        private const string CONFIGURATION_KEY_ROUTER_EXCHANGE_REQUEST_NAME = "router:exchanges:requests:name";
        private const string CONFIGURATION_KEY_ROUTER_EXCHANGE_REQUEST_ROUTEKEY = "router:exchanges:requests:route-key";
        private const string CONFIGURATION_KEY_ROUTER_EXCHANGE_RESPONSE_NAME = "router:exchanges:responses:name";
        private const string CONFIGURATION_KEY_ROUTER_EXCHANGE_RESPONSE_ROUTEKEY = "router:exchanges:responses:route-key";

        #endregion

        #region Router Queues
        
        private const string CONFIGURATION_KEY_ROUTER_QUEUE_HOST = "router:queues:host:host";
        private const string CONFIGURATION_KEY_ROUTER_QUEUE_USERNAME = "router:queues:host:username";
        private const string CONFIGURATION_KEY_ROUTER_QUEUE_PASSWORD = "router:queues:host:password";
        private const string CONFIGURATION_KEY_ROUTER_QUEUE_VHOST = "router:queues:host:vhost";

        private const string CONFIGURATION_KEY_ROUTER_QUEUE_REQUEST_NAME = "router:queues:requests:name";
        private const string CONFIGURATION_KEY_ROUTER_QUEUE_REQUEST_ROUTEKEY = "router:queues:requests:route-key";
        private const string CONFIGURATION_KEY_ROUTER_QUEUE_RESPONSE_NAME = "router:queues:responses:name";
        private const string CONFIGURATION_KEY_ROUTER_QUEUE_RESPONSE_ROUTEKEY = "router:queues:responses:route-key";

        #endregion

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
        /// The configuration
        /// </summary>
        private IConfigurationHandler _config;

        /// <summary>
        /// The connection manager
        /// </summary>
        private IWebSocketConnectionManager _connectionManager;

        /// <summary>
        /// The router exchange connection
        /// </summary>
        private IEventedExchangeGenerator _routerExchangeGenerator;

        /// <summary>
        /// The router queue connection
        /// </summary>
        private IEventedQueueGenerator _routerQueueGenerator;

        /// <summary>
        /// The request exchange identifier
        /// </summary>
        private string _requestExchangeId;

        /// <summary>
        /// The request queue identifier
        /// </summary>
        private string _requestQueueId;

        /// <summary>
        /// The response exchange identifier
        /// </summary>
        private string _responseExchangeId;

        /// <summary>
        /// The response queue identifier
        /// </summary>
        private string _responseQueueId;        

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
            
            CreateConnectionResources();
            CreateRequestResources();
            CreateResponseResources();

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

            _routerExchangeGenerator.Publish(_requestExchangeId, signal);
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
            
            foreach (string entityId in entities)
                BroadcastSignal(entityId, signal);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Creates the connection resources.
        /// </summary>
        private void CreateConnectionResources()
        {
            //Create exchange connection
            _routerExchangeGenerator = new RabbitEventedExchangeGenerator(_dependencyUtility, new ParameterList
            {
                ["Host"] = _config.Get(CONFIGURATION_KEY_ROUTER_EXCHANGE_HOST),
                ["VirtualHost"] = _config.Get(CONFIGURATION_KEY_ROUTER_EXCHANGE_VHOST),
                ["Username"] = _config.Get(CONFIGURATION_KEY_ROUTER_EXCHANGE_USERNAME),
                ["Password"] = _config.Get(CONFIGURATION_KEY_ROUTER_EXCHANGE_PASSWORD)
            });
            
            //Create queue connection
            _routerQueueGenerator = new RabbitEventedQueueGenerator(_dependencyUtility, new ParameterList
            {
                ["Host"] = _config.Get(CONFIGURATION_KEY_ROUTER_QUEUE_HOST),
                ["VirtualHost"] = _config.Get(CONFIGURATION_KEY_ROUTER_QUEUE_VHOST),
                ["Username"] = _config.Get(CONFIGURATION_KEY_ROUTER_QUEUE_USERNAME),
                ["Password"] = _config.Get(CONFIGURATION_KEY_ROUTER_QUEUE_PASSWORD)
            });
        }

        /// <summary>
        /// Creates the request resources.
        /// </summary>
        private void CreateRequestResources()
        {
            _requestExchangeId = _config.Get(CONFIGURATION_KEY_ROUTER_EXCHANGE_REQUEST_NAME);
            _requestQueueId = _config.Get(CONFIGURATION_KEY_ROUTER_QUEUE_REQUEST_NAME);

            //Create exchange
            _routerExchangeGenerator.AddExchange<ISignal>(new ParameterList
            {
                ["ExchangeId"] = _requestExchangeId,
                ["RouteKey"] = _config.Get(CONFIGURATION_KEY_ROUTER_EXCHANGE_REQUEST_ROUTEKEY),
                ["ExchangeType"] = ExchangeType.Fanout
            });

            //Create queue
            _routerQueueGenerator.AddQueue<ISignal>(new ParameterList
            {
                ["ExchangeId"] = _requestExchangeId,
                ["QueueId"] = _requestQueueId,
                ["RouteKey"] = _config.Get(CONFIGURATION_KEY_ROUTER_QUEUE_REQUEST_ROUTEKEY),
                ["Durable"] = true,
                ["Transient"] = false,
                ["AutoDelete"] = true
            });

            //Attach request handler
            _routerQueueGenerator.AddDequeueEvent<ISignal>(_requestQueueId, _requestExchangeId, HandleDequeuedRequest);
        }

        /// <summary>
        /// Creates the response resources.
        /// </summary>
        private void CreateResponseResources()
        {
            _responseExchangeId = _config.Get(CONFIGURATION_KEY_ROUTER_EXCHANGE_RESPONSE_NAME);
            _responseQueueId = _config.Get(CONFIGURATION_KEY_ROUTER_QUEUE_RESPONSE_NAME);

            //Create exchange
            _routerExchangeGenerator.AddExchange<ISignal>(new ParameterList
            {
                ["ExchangeId"] = _responseExchangeId,
                ["RouteKey"] = _config.Get(CONFIGURATION_KEY_ROUTER_EXCHANGE_RESPONSE_ROUTEKEY),
                ["ExchangeType"] = ExchangeType.Fanout
            });

            //Create queue
            _routerQueueGenerator.AddQueue<ISignal>(new ParameterList
            {
                ["ExchangeId"] = _responseExchangeId,
                ["QueueId"] = _responseQueueId,
                ["RouteKey"] = _config.Get(CONFIGURATION_KEY_ROUTER_QUEUE_RESPONSE_ROUTEKEY),
                ["Durable"] = true,
                ["Transient"] = false,
                ["AutoDelete"] = true
            });

            //Attach response handler
            _routerQueueGenerator.AddDequeueEvent<ISignal>(_responseQueueId, _responseExchangeId, HandleDequeuedResponse);
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
        /// Handles the request.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="signal">The signal.</param>
        /// <exception cref="InvalidExtensionException"></exception>
        private void HandleDequeuedRequest(object sender, ISignal signal)
        {
            IWebSocketConnection[] connections = _connectionManager.GetConnectionsByClientIdentifier(signal.Sender);
            if (!connections.Any())
                return;

            IExtension extension = GetExtension(signal);
            if (extension == null)
                throw new InvalidExtensionException($"Attempted to route an to an invalid extension. Route: {signal.Route}");
            
            _logger.Debug(signal.Format("Request"));

            ISignal response = extension.ExecuteAction(signal.Action, signal);

            if (response == null)
                return;

            _routerExchangeGenerator.Publish(_responseExchangeId, response);
        }

        /// <summary>
        /// Handles the response.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="signal">The signal.</param>
        private void HandleDequeuedResponse(object sender, ISignal signal)
        {
            IWebSocketConnection[] connections = _connectionManager.GetConnectionsByClientIdentifier(signal.Recipient);
            if (!connections.Any())
                return;

            _logger.Debug(signal.Format("Response"));

            foreach (IWebSocketConnection connection in connections)
                connection.SendText(signal);
        }

        #endregion
    }
}