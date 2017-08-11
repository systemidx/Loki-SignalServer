using System.Collections.Generic;

namespace Loki.SignalServer.Extensions.Interfaces
{
    public interface IExtensionLoader
    {
        void LoadExtensions();
        HashSet<IExtension> Extensions { get; }
    }
}