using System;

namespace Loki.SignalServer.Interfaces.Exceptions
{
    public class ExtensionNotInitializedException : Exception
    {
        private readonly string _extensionName;

        public override string Message => $"Extension: {_extensionName} has not been initialized.";

        public ExtensionNotInitializedException(string extensionName)
        {
            _extensionName = extensionName;
        }
    }
}
