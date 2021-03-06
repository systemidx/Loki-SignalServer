﻿using System;
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
        #region Constants

        /// <summary>
        /// The extension configuration key
        /// </summary>
        private const string EXTENSION_CONFIGURATION_KEY = "extensions";

        #endregion

        #region Readonly Variables

        /// <summary>
        /// The dependency utility
        /// </summary>
        private readonly IDependencyUtility _dependencyUtility;

        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The configuration
        /// </summary>
        private readonly IConfigurationHandler _config;

        /// <summary>
        /// The extension types
        /// </summary>
        private readonly Dictionary<TypeInfo, string> _extensionTypes = new Dictionary<TypeInfo, string>();

        #endregion

        #region Properties

        /// <summary>
        /// Gets the extensions.
        /// </summary>
        /// <value>
        /// The extensions.
        /// </value>
        public HashSet<IExtension> Extensions { get; private set; }

        #endregion

        #region Constructor

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

        #endregion

        #region Public Methods

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

                LoadAssembly(path, name);
            }

            foreach (var extensionType in _extensionTypes)
            {
                IExtension extension = Activator.CreateInstance(extensionType.Key.AsType(), extensionType.Value, _dependencyUtility) as IExtension;
                if (extension == null)
                    throw new InvalidExtensionException(extensionType.Value, extensionType.Key.AssemblyQualifiedName);

                extension.Initialize();

                extensions.Add(extension);
            }
            
            Extensions = extensions;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Loads the assembly.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="name">The name.</param>
        private void LoadAssembly(string path, string name)
        {
            FileInfo[] dlls = new DirectoryInfo(Path.GetDirectoryName(path)).GetFiles("*.dll");

            foreach (FileInfo dll in dlls)
            {
                Assembly asm;

                try
                {
                    asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(dll.FullName);
                }
                catch (FileLoadException)
                {
                    continue;
                }

                _logger.Debug($"Loading assembly: {asm.FullName}");

                try
                {
                    TypeInfo type = asm.DefinedTypes?.FirstOrDefault(x => x.ImplementedInterfaces.Contains(typeof(IExtension)) && !x.IsAbstract);
                    if (type == null)
                        continue;

                    _extensionTypes.Add(type, name);
                }
                catch (ReflectionTypeLoadException)
                {
                }
            }
        }

        #endregion
    }
}