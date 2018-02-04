using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Loki.Interfaces.Dependency;
using Loki.Interfaces.Logging;
using Loki.SignalServer.Interfaces.Configuration;
using Loki.SignalServer.Interfaces.Exceptions;
using Microsoft.Extensions.Configuration;

namespace Loki.SignalServer.Common.Configuration
{
    public class ConfigurationHandler : IConfigurationHandler
    {
        #region Readonly Variables

        /// <summary>
        /// The dependency utility
        /// </summary>
        private readonly IDependencyUtility _dependencyUtility;

        /// <summary>
        /// The builder
        /// </summary>
        private readonly ConfigurationBuilder _builder = new ConfigurationBuilder();

        #endregion

        #region Private Variables

        /// <summary>
        /// The logger
        /// </summary>
        private ILogger _logger;

        /// <summary>
        /// The configuration
        /// </summary>
        private IConfigurationRoot _configuration;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationHandler"/> class.
        /// </summary>
        /// <param name="dependencyUtility">The dependency utility.</param>
        /// <param name="configurationPath">The configuration path.</param>
        public ConfigurationHandler(IDependencyUtility dependencyUtility, string configurationPath)
        {
            _dependencyUtility = dependencyUtility;
            _logger = dependencyUtility.Resolve<ILogger>();

            AddConfigurationFile(configurationPath);
        }

        #endregion

        #region Public Methods
        
        /// <summary>
        /// Gets the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public string Get(string key)
        {
            return _configuration[key];
        }

        /// <summary>
        /// Gets the enum.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public T Get<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ConfigurationItemMissingException(nameof(key));

            string value = _configuration[key];
            
            Type type = typeof(T);

            try
            {
                if (type.GetTypeInfo().IsEnum)
                    return (T) System.Enum.Parse(type, value, true);
                return (T) Convert.ChangeType(value, type);
            }
            catch (InvalidCastException ex)
            {
                if (_logger == null)
                    _logger = _dependencyUtility.Resolve<ILogger>();

                _logger.Error(ex);
            }
            catch (ArgumentNullException ex)
            {
                if (_logger == null)
                    _logger = _dependencyUtility.Resolve<ILogger>();

                _logger.Error(ex);

                throw;
            }

            return default(T);
        }

        /// <summary>
        /// Gets the sections.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public IEnumerable<IConfigurationSection> GetSections(string key)
        {
            return _configuration.GetSection(key).GetChildren();
        }

        #endregion

        #region Private Methods
        
        /// <summary>
        /// Adds the configuration file. The configuration file *must be in JSON format*.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <exception cref="System.IO.FileNotFoundException">filePath</exception>
        private void AddConfigurationFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException(nameof(filePath));

            _builder.AddJsonFile(filePath);
            _configuration = _builder.Build();
        }

        #endregion
    }
}
