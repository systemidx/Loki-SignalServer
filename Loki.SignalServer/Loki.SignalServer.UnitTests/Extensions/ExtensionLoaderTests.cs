using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Loki.Interfaces.Dependency;
using Loki.Interfaces.Logging;
using Loki.Server.Dependency;
using Loki.SignalServer.Configuration;
using Loki.SignalServer.Extensions;
using Loki.SignalServer.Extensions.Interfaces;
using Loki.SignalServer.Interfaces.Configuration;
using Loki.SignalServer.Interfaces.Exceptions;
using Moq;
using Xunit;

namespace Loki.SignalServer.UnitTests.Extensions
{
    public class ExtensionLoaderTests
    {
        [Fact]
        public void ExtensionLoaderThrowsExceptionWhenFileDoesNotExist()
        {
            string json = "{\"host\":\"0.0.0.0\",\"port\":1337,\"extensions\":{\"extension1\":{\"path\":\"\",\"config\":{\"key\":\"value\"}}}}";
            IConfigurationHandler config = GetConfigurationHandler(json, out string path);

            IDependencyUtility dependencyUtility = new DependencyUtility();
            dependencyUtility.Register<ILogger>(new Mock<ILogger>().Object);
            dependencyUtility.Register<IConfigurationHandler>(config);

            IExtensionLoader loader = new ExtensionLoader(dependencyUtility);

            try
            {
                Assert.Throws(typeof(ConfigurationItemMissingException), () => loader.LoadExtensions());
            }
            finally
            {
                File.Delete(path);
            }
        }

        private IConfigurationHandler GetConfigurationHandler(string json, out string path)
        {
            path = $"{Path.GetTempPath()}/{DateTime.UtcNow.Ticks}.json";

            File.WriteAllText(path, json, Encoding.UTF8);
            
            return new ConfigurationHandler(path);
        }
    }
}
