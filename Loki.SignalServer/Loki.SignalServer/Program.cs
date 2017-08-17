using System;
using System.Net;
using System.Runtime.Loader;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Loki.Interfaces;
using Loki.Interfaces.Dependency;
using Loki.Interfaces.Logging;
using Loki.Interfaces.Security;
using Loki.Server.Dependency;
using Loki.Server.Logging;
using Loki.Server.Security;
using Loki.SignalServer.Common.Queues;
using Loki.SignalServer.Configuration;
using Loki.SignalServer.Extensions;
using Loki.SignalServer.Extensions.Interfaces;
using Loki.SignalServer.Interfaces.Configuration;
using Loki.SignalServer.Interfaces.Queues;
using Loki.SignalServer.Interfaces.Router;
using Loki.SignalServer.Router;
using Newtonsoft.Json;

namespace Loki.SignalServer
{
    class Program
    {
        private const string HOST_CONFIGURATION_KEY = "host";
        private const string PORT_CONFIGURATION_KEY = "port";
        private const string PFX_PATH_CONFIGURATION_KEY = "pfx:path";
        private const string PFX_KEY_CONFIGURATION_KEY = "pfx:key";

        private static IServer _server;
        private static IDependencyUtility _dependencyUtility;
        private static IConfigurationHandler _config;
        private static ILogger _logger;
        private static IEventedQueueHandler<ISignal> _queueHandler;

        static void Main(string[] args)
        {
            _dependencyUtility = new DependencyUtility();

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

            //Start the server
            using (_server = new Server.Server("MyServerName", host, port, _dependencyUtility))
            {
                _logger.Info($"Listening on {host}:{port}");

                //Disable Nagle's Algorithm
                _server.NoDelay = true;

                //Start the queue handler
                _queueHandler.Start();

                //Start listening and blocking the main thread
                _server.Run();
            }
        }
        
        private static void UnloadServer(AssemblyLoadContext assemblyLoadContext)
        {
            _server?.Stop();
            Environment.Exit(0);
        }

        private static void HandleConfiguration()
        {
            _config = new ConfigurationHandler("configuration.json");

            _dependencyUtility.Register<JsonSerializerSettings>(new JsonSerializerSettings {
                TypeNameHandling = TypeNameHandling.All
            });
            _dependencyUtility.Register<IConfigurationHandler>(_config);
        }

        private static void HandleSecurityContainer()
        {
            X509Certificate2 certificate;

            using (X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadOnly);

                certificate = store.Certificates.Count == 0 ? null : store.Certificates[0];
            }
            
            _dependencyUtility.Register<ISecurityContainer>(new SecurityContainer(certificate, SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12, false, true, false));
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

            _dependencyUtility.Register<ILogger>(_logger);
        }

        private static void HandleSignalRouter()
        {
            _dependencyUtility.Register<ISignalRouter>(new SignalRouter(_dependencyUtility));
        }

        private static void HandleExtensionLoader()
        {
            IExtensionLoader extensionLoader = new ExtensionLoader(_dependencyUtility);
            extensionLoader.LoadExtensions();

            _dependencyUtility.Register<IExtensionLoader>(extensionLoader);
        }
    }
}