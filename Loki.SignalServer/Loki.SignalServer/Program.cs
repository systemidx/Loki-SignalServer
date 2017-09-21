using System;
using System.IO;
using System.Net;
using System.Runtime.Loader;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Loki.Interfaces;
using Loki.Interfaces.Dependency;
using Loki.Interfaces.Logging;
using Loki.Interfaces.Security;
using Loki.Server;
using Loki.Server.Dependency;
using Loki.Server.Logging;
using Loki.Server.Security;
using Loki.SignalServer.Common.Cache;
using Loki.SignalServer.Common.Configuration;
using Loki.SignalServer.Common.Queues;
using Loki.SignalServer.Extensions;
using Loki.SignalServer.Extensions.Interfaces;
using Loki.SignalServer.Interfaces.Cache;
using Loki.SignalServer.Interfaces.Configuration;
using Loki.SignalServer.Interfaces.Queues;
using Loki.SignalServer.Interfaces.Router;
using Loki.SignalServer.Router;
using Newtonsoft.Json;

namespace Loki.SignalServer
{
    class Program
    {
        private const string HOST_CONFIGURATION_KEY = "connection:host";
        private const string PORT_CONFIGURATION_KEY = "connection:port";
        private const string PFX_PATH_CONFIGURATION_KEY = "connection:pfx:path";
        private const string PFX_KEY_CONFIGURATION_KEY = "connection:pfx:key";
        private const string LOG_LEVEL_CONFIGURATION_KEY = "log-level";

        private static IServer _server;
        private static IDependencyUtility _dependencyUtility;
        private static IConfigurationHandler _config;
        private static ILogger _logger;
        private static IEventedQueueHandler<ISignal> _queueHandler;
        private static ICacheHandler _cacheHandler;
        private static ISignalRouter _router;

        static void Main(string[] args)
        {
            _dependencyUtility = new DependencyUtility();

            HandleCacheHandler();
            HandleConfiguration();
            HandleSecurityContainer();
            HandleLogger();
            HandleQueueHandler();
            HandleSignalRouter();
            HandleExtensionLoader();

            //Set port and host from configuration
            int port = Convert.ToInt32(_config.Get(PORT_CONFIGURATION_KEY));
            IPAddress host = IPAddress.Parse(_config.Get(HOST_CONFIGURATION_KEY));

            //Hook into our closing event
            AssemblyLoadContext.Default.Unloading += UnloadServer;
            Console.CancelKeyPress += (sender, eventArgs) => UnloadServer(AssemblyLoadContext.Default);

            //Start the server
            using (_server = new WebSocketServer("MyServerName", host, port, _dependencyUtility, 4))
            {
                _logger.Info($"Listening on {host}:{port}");
                
                //Initialize the router
                _router.Initialize();

                //Disable Nagle's Algorithm
                _server.NoDelay = true;

                //Start the queue handler
                _queueHandler.Start();

                //Start listening and blocking the main thread
                _server.Run(false);

                Console.ReadLine();
            }
        }

        /// <summary>
        /// Unloads the server.
        /// </summary>
        /// <param name="assemblyLoadContext">The assembly load context.</param>
        private static void UnloadServer(AssemblyLoadContext assemblyLoadContext)
        {
            _server?.Stop();

            Environment.Exit(0);
        }

        /// <summary>
        /// Handles the configuration.
        /// </summary>
        private static void HandleConfiguration()
        {
            _config = new ConfigurationHandler("configuration.json");

            _dependencyUtility.Register<JsonSerializerSettings>(new JsonSerializerSettings {
                TypeNameHandling = TypeNameHandling.All
            });
            _dependencyUtility.Register<IConfigurationHandler>(_config);
        }

        /// <summary>
        /// Handles the security container.
        /// </summary>
        private static void HandleSecurityContainer()
        {
            string pfxPath = _config.Get(PFX_PATH_CONFIGURATION_KEY);
            if (!File.Exists(pfxPath))
                return;

            X509Certificate2 certificate = null;
#if RELEASE
            certificate = new X509Certificate2(pfxPath, _config.Get(PFX_KEY_CONFIGURATION_KEY));
#endif
            _dependencyUtility.Register<ISecurityContainer>(new SecurityContainer(certificate, SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12, false, true, true));
        }

        /// <summary>
        /// Handles the cache handler.
        /// </summary>
        private static void HandleCacheHandler()
        {
            _cacheHandler = new CacheHandler(_dependencyUtility);

            _dependencyUtility.Register<ICacheHandler>(_cacheHandler);
        }

        private static void HandleQueueHandler()
        {
            _queueHandler = new EventedQueueHandler<ISignal>(_dependencyUtility);

            _dependencyUtility.Register<IEventedQueueHandler<ISignal>>(_queueHandler);
        }

        private static void HandleLogger()
        {
            _logger = new Logger();
            _logger.OnError += (sender, args) => Console.WriteLine($"[{args.EventTimeStamp}]\tERROR\t{args.Exception}");
            _logger.OnDebug += (sender, args) => Console.WriteLine($"[{args.EventTimeStamp}]\tDEBUG\t{args.Message}");
            _logger.OnWarn += (sender, args) => Console.WriteLine($"[{args.EventTimeStamp}]\tWARN\t{args.Message}");
            _logger.OnInfo += (sender, args) => Console.WriteLine($"[{args.EventTimeStamp}]\tINFO\t{args.Message}");
            _logger.LogLevel = _config.Get<LogLevel>(LOG_LEVEL_CONFIGURATION_KEY);

            _dependencyUtility.Register<ILogger>(_logger);
        }
        
        private static void HandleSignalRouter()
        {
            _router = new SignalRouter(_dependencyUtility);
            _dependencyUtility.Register<ISignalRouter>(_router);
        }

        private static void HandleExtensionLoader()
        {
            IExtensionLoader extensionLoader = new ExtensionLoader(_dependencyUtility);
            extensionLoader.LoadExtensions();

            _dependencyUtility.Register<IExtensionLoader>(extensionLoader);
        }
    }
}