using System;
using System.Collections.Generic;
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

        public HashSet<IExtension> Extensions { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtensionLoader"/> class.
        /// </summary>
        /// <param name="dependencyUtility">The dependency utility.</param>
        public ExtensionLoader(IDependencyUtility dependencyUtility)
        {
            _dependencyUtility = dependencyUtility;
            _logger = _dependencyUtility.Resolve<ILogger>();
            _config = _dependencyUtility.Resolve<IConfigurationHandler>();
        }

        /// <summary>
        /// Loads the extensions.
        /// </summary>
        /// <exception cref="ConfigurationItemMissingException"></exception>
        /// <exception cref="InvalidExtensionException">
        /// </exception>
        public void LoadExtensions()
        {
            IConfigurationSection[] extensionConfigurations = _config.GetSections(EXTENSION_CONFIGURATION_KEY).ToArray();
            if (extensionConfigurations.Length == 0)
                return;

            HashSet<IExtension> extensions = new HashSet<IExtension>();
            foreach (IConfigurationSection extensionConfiguration in extensionConfigurations)
            {
                string name = extensionConfiguration.Key;
                string path = _config.Get($"{extensionConfiguration.Path}:path");

                _logger.Debug($"Loading extension: {name}");

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

                extensions.Add(extension);
            }

            Extensions = extensions;
        }
    }
}