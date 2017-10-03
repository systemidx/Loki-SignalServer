using System.Collections.Generic;

namespace Loki.SignalServer.Extensions.Interfaces
{
    public interface IExtensionLoader
    {
        /// <summary>
        /// Loads the extensions.
        /// </summary>
        void LoadExtensions();

        /// <summary>
        /// Gets the extensions.
        /// </summary>
        /// <value>
        /// The extensions.
        /// </value>
        HashSet<IExtension> Extensions { get; }
    }
}