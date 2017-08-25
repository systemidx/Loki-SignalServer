using System;

namespace Loki.SignalServer.Interfaces.Exceptions
{
    public class InvalidExtensionException : Exception
    {
        private readonly string _extensionName;
        private readonly string _extensionPath;
        private readonly string _message;

        public override string Message => _message ?? $"Failed to load extension {_extensionName} from {_extensionPath}";

        public InvalidExtensionException(string extensionName, string extensionPath)
        {
            _extensionName = extensionName;
            _extensionPath = extensionPath;
        }

        public InvalidExtensionException(string message)
        {
            _message = message;
        }
    }
}
