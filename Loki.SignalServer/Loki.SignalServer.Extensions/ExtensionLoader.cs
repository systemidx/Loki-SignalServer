using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Loki.Interfaces.Dependency;
using Loki.Interfaces.Logging;
using Loki.SignalServer.Extensions.Interfaces;
using Loki.SignalServer.Interfaces.Configuration;
using Loki.SignalServer.Interfaces.Exceptions;
using Microsoft.Extensions.Configuration;

namespace Loki.SignalServer.Extensions
{
    public class ExtensionLoader : IExtensionLoader
    {
        private const string EXTENSION_CONFIGURATION_KEY = "extensions";

        private readonly IDependencyUtility _dependencyUtility;
        private readonly ILogger _logger;
        private readonly IConfigurationHandler _config;

        public ExtensionLoader(IDependencyUtility dependencyUtility)
        {
            _dependencyUtility = dependencyUtility;
            _logger = _dependencyUtility.Resolve<ILogger>();
            _config = _dependencyUtility.Resolve<IConfigurationHandler>();
        }

        public void LoadExtensions()
        {
            IConfigurationSection[] extensions = _config.GetSections(EXTENSION_CONFIGURATION_KEY).ToArray();
            if (extensions.Length == 0)
                return;

            foreach (IConfigurationSection extensionConfiguration in extensions)
            {
                string name = extensionConfiguration.Key;
                string path = _config.Get($"{extensionConfiguration.Path}:path");

                _logger.Debug($"Attempting to load extension {name} from {path}");

                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                    throw new ConfigurationItemMissingException($"{extensionConfiguration.Path}:path");

                Assembly assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
                if (assembly == null)
                    throw new InvalidExtensionException(name, path);

                TypeInfo type = assembly.DefinedTypes.FirstOrDefault(x => x.ImplementedInterfaces.Contains(typeof(IExtension)));
                if (type == null)
                    throw new InvalidExtensionException(name, path);

                IExtension extension = Activator.CreateInstance(type.AsType(), name, _dependencyUtility) as IExtension;
                if (extension == null)
                    throw new InvalidExtensionException(name, path);
            }
        }
    }
}