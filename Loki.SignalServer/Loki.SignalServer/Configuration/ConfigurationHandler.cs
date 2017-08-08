using System.Collections.Generic;
using System.IO;
using Loki.SignalServer.Interfaces.Configuration;
using Microsoft.Extensions.Configuration;

namespace Loki.SignalServer.Configuration
{
    public class ConfigurationHandler : IConfigurationHandler
    {
        private readonly ConfigurationBuilder _builder = new ConfigurationBuilder();

        private IConfigurationRoot _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationHandler"/> class.
        /// </summary>
        /// <param name="configurationPath">The configuration path.</param>
        public ConfigurationHandler(string configurationPath)
        {
            AddConfigurationFile(configurationPath);
        }

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
        /// Gets the sections.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public IEnumerable<IConfigurationSection> GetSections(string key)
        {
            return _configuration.GetSection(key).GetChildren();
        }
    }
}
