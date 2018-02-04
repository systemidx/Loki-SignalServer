using System;

namespace Loki.SignalServer.Interfaces.Exceptions
{
    public class DependencyNotInitException : Exception
    {
        private readonly string _dependency;

        public override string Message => $"Dependency {_dependency} has not been initialized.";

        public DependencyNotInitException(string dependency)
        {
            _dependency = dependency;
        }
    }
}
