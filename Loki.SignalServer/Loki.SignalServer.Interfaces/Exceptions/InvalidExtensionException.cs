using System;

namespace Loki.SignalServer.Interfaces.Exceptions
{
    public class InvalidExtensionException : Exception
    {
        private readonly string _extensionName;
        private readonly string _extensionPath;

        public override string Message => $"Failed to load extension {_extensionName} from {_extensionPath}";

        public InvalidExtensionException(string extensionName, string extensionPath)
        {
            _extensionName = extensionName;
            _extensionPath = extensionPath;
        }
    }
}
