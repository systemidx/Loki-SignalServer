using System;

namespace Loki.SignalServer.Interfaces.Exceptions
{
    public class ConfigurationItemMissingException : Exception
    {
        /// <summary>
        /// The configuration key
        /// </summary>
        private readonly string _configurationKey;

        /// <summary>
        /// Gets a message that describes the current exception.
        /// </summary>
        public override string Message => $"Invalid configuration key: {_configurationKey}";

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationItemMissingException"/> class.
        /// </summary>
        /// <param name="configurationKey">The configuration key.</param>
        public ConfigurationItemMissingException(string configurationKey)
        {
            _configurationKey = configurationKey;
        }
    }
}
