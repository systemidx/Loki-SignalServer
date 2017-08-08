using System;
using System.IO;
using System.Net;
using System.Runtime.Loader;
using System.Security.Authentication;
using Loki.Common.Events;
using Loki.Interfaces;
using Loki.Interfaces.Dependency;
using Loki.Interfaces.Logging;
using Loki.Interfaces.Security;
using Loki.Server.Dependency;
using Loki.Server.Logging;
using Loki.Server.Security;
using Loki.SignalServer.Configuration;
using Loki.SignalServer.Extensions;
using Loki.SignalServer.Extensions.Interfaces;
using Loki.SignalServer.Interfaces.Configuration;
using Microsoft.Extensions.Configuration;

namespace Loki.SignalServer
{
    class Program
    {
        private const string HOST_CONFIGURATION_KEY = "host";
        private const string PORT_CONFIGURATION_KEY = "port";
        private const string PFX_PATH_CONFIGURATION_KEY = "pfx:path";
        private const string PFX_KEY_CONFIGURATION_KEY = "pfx:key";

        private static IServer _server;

        private static IConfigurationHandler _config;

        static void Main(string[] args)
        {
            IDependencyUtility dependencyUtility = new DependencyUtility();

            //Get configuration
            _config = new ConfigurationHandler("configuration.json");
            dependencyUtility.Register<IConfigurationHandler>(_config);

            //Get security container if available
            ISecurityContainer securityContainer = GenerateSecurityContainer();
            dependencyUtility.Register<ISecurityContainer>(securityContainer);

            //Create our log wrapper and events
            ILogger logger = new Logger();
            logger.OnError += OnError;
            logger.OnDebug += OnDebug;
            logger.OnWarn += OnWarn;
            logger.OnInfo += OnInfo;
            dependencyUtility.Register<ILogger>(logger);

            //Create and register extension loader
            IExtensionLoader extensionLoader = new ExtensionLoader(dependencyUtility);
            dependencyUtility.Register<IExtensionLoader>(extensionLoader);

            extensionLoader.LoadExtensions();

            //Set port and host from configuration
            int port = Convert.ToInt32(_config.Get(PORT_CONFIGURATION_KEY));
            IPAddress host = IPAddress.Parse(_config.Get(HOST_CONFIGURATION_KEY));

            //Hook into our closing event
            AssemblyLoadContext.Default.Unloading += UnloadServer;

            //Start the server
            using (_server = new Server.Server("MyServerName", host, port, dependencyUtility))
            {
                //Disable Nagle's Algorithm
                _server.NoDelay = true;

                //Start listening and blocking the main thread
                _server.Run();
            }
        }

        private static void UnloadServer(AssemblyLoadContext assemblyLoadContext)
        {
            _server?.Stop();
            Environment.Exit(0);
        }

        private static void OnError(object sender, LokiErrorEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[{e.EventTimeStamp}]\tERROR\t{e.Exception}");
            Console.ResetColor();
        }

        private static void OnDebug(object sender, LokiDebugEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[{e.EventTimeStamp}]\tDEBUG\t{e.Message}");
            Console.ResetColor();
        }

        private static void OnInfo(object sender, LokiInfoEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"[{e.EventTimeStamp}]\tDEBUG\t{e.Message}");
            Console.ResetColor();
        }
        private static void OnWarn(object sender, LokiWarnEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[{e.EventTimeStamp}]\tWARN\t{e.Message}");
            Console.ResetColor();
        }

        private static ISecurityContainer GenerateSecurityContainer()
        {
            return new SecurityContainer(null, SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12, false, true, false);
        }
    }
}