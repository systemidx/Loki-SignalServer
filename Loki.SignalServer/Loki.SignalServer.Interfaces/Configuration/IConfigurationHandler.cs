using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Loki.SignalServer.Interfaces.Configuration
{
    public interface IConfigurationHandler
    {
        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        string Get(string key);

        /// <summary>
        /// Gets the enum.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        T Get<T>(string key);

        /// <summary>
        /// Gets the sections.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        IEnumerable<IConfigurationSection> GetSections(string key);
    }
}