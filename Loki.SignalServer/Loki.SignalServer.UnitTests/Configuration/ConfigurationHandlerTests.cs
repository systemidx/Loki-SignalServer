using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Loki.Interfaces.Dependency;
using Loki.Interfaces.Logging;
using Loki.Server.Dependency;
using Loki.SignalServer.Configuration;
using Loki.SignalServer.Interfaces.Configuration;
using Microsoft.Extensions.Configuration;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Loki.SignalServer.UnitTests.Configuration
{
    public class ConfigurationHandlerTests
    {
        #region Mocks

        private class TestConfiguration
        {
            public string TestVariable { get; set; }
            public string[] TestSimpleArray { get; set; }
            public TestSubObject SubObject { get; set; }
            public TestSubObject[] SubObjectArray { get; set; }
            public TestEnum TestEnum { get; set; }

            public class TestSubObject
            {
                public readonly string TestField;

                public TestSubObject(string testField)
                {
                    TestField = testField;
                }
            }
        }

        private enum TestEnum
        {
            TestA,
            TestB
        }

        #endregion

        #region Helpers

        private void WriteExecuteDelete(Action<IConfigurationHandler> action, string json)
        {
            IDependencyUtility dependencyUtility = new DependencyUtility();
            dependencyUtility.Register(new Mock<ILogger>().Object);

            string path = $"{Path.GetTempPath()}/{DateTime.UtcNow.Ticks}.json";

            File.WriteAllText(path, json, Encoding.UTF8);

            IConfigurationHandler config = new ConfigurationHandler(path);

            try
            {
                action.Invoke(config);
            }
            finally
            {
                File.Delete(path);
            }
        }

        #endregion

        #region Tests

        [Fact]
        public void ConfigurationHandlerGetsCorrectValueForArray()
        {
            string json = JsonConvert.SerializeObject(new TestConfiguration { TestSimpleArray = new[] { "a", "b" } });

            WriteExecuteDelete(handler =>
            {
                Assert.Equal("a", handler.Get("TestSimpleArray:0"));
                Assert.Equal("b", handler.Get("TestSimpleArray:1"));
            }, json);
        }

        [Fact]
        public void ConfigurationHandlerGetsCorrectValueForKey()
        {
            string json = JsonConvert.SerializeObject(new TestConfiguration { TestVariable = "value" });

            WriteExecuteDelete(handler =>
            {
                Assert.Equal("value", handler.Get("TestVariable"));
            }, json);
        }

        [Fact]
        public void ConfigurationHandlerGetsCorrectValueForSubObjectArray()
        {
            string json = JsonConvert.SerializeObject(
                new TestConfiguration
                {
                    SubObjectArray = new[] {
                        new TestConfiguration.TestSubObject("TestFieldA"),
                        new TestConfiguration.TestSubObject("TestFieldB")
                    }
                }
            );

            WriteExecuteDelete(handler =>
            {
                Assert.Equal("TestFieldA", handler.Get("SubObjectArray:0:TestField"));
                Assert.Equal("TestFieldB", handler.Get("SubObjectArray:1:TestField"));
            }, json);
        }

        [Fact]
        public void ConfigurationHandlerGetsCorrectValueForSubObjectField()
        {
            var json = JsonConvert.SerializeObject(new TestConfiguration { SubObject = new TestConfiguration.TestSubObject("TestFieldValue") });

            WriteExecuteDelete(handler =>
            {
                Assert.Equal("TestFieldValue", handler.Get("SubObject:TestField"));
            }, json);
        }

        [Fact]
        public void ConfigurationHandlerHandlesPseudoArrays()
        {
            string json = "{\"host\":\"0.0.0.0\",\"port\":1337,\"extensions\":{\"0\":{\"name\":\"extension1\",\"path\":\"C:\\\\Folder\",\"config\":{\"key\":\"value\"}},\"1\":{\"path\":\"C:\\\\Folder2\",\"config\":{\"key\":2}}}}";

            WriteExecuteDelete(handler =>
            {
                Assert.Equal("extension1", handler.Get("extensions:0:name"));
            }, json);
        }

        [Fact]
        public void ConfigurationHandlerHasSearchableSections()
        {
            string json = "{\"host\":\"0.0.0.0\",\"port\":1337,\"extensions\":{\"extension1\":{\"path\":\"C:\\\\Folder\",\"config\":{\"key\":\"value\"}},\"extension2\":{\"path\":\"C:\\\\Folder2\",\"config\":{\"key\":2}}}}";

            WriteExecuteDelete(handler =>
            {
                IConfigurationSection[] extensions = handler.GetSections("extensions").ToArray();

                Assert.Equal("extension1", extensions[0].Key);
                Assert.Equal("extension2", extensions[1].Key);
                Assert.Equal("C:\\Folder", handler.Get($"{extensions[0].Path}:path"));
            }, json);
        }

        [Fact]
        public void ConfigurationHandlerThrowsExceptionForMissingFile()
        {
            Assert.Throws(typeof(FileNotFoundException), () => new ConfigurationHandler(null));
        }

        [Fact]
        public void ConfigurationHandlerReturnsCorrectEnum()
        {
            string json = JsonConvert.SerializeObject(new TestConfiguration { TestEnum = TestEnum.TestB });
            WriteExecuteDelete(handler =>
            {
                Assert.Equal(TestEnum.TestB, handler.Get<TestEnum>("TestEnum"));
            }, json);
        }

        [Fact]
        public void ConfigurationHandlerReturnsCorrectEnumWithNonSerializedString()
        {
            string json = "{\"TestEnum\":\"Testb\"}";
            WriteExecuteDelete(handler =>
            {
                Assert.Equal(TestEnum.TestB, handler.Get<TestEnum>("TestEnum"));
            }, json);
        }

        [Fact]
        public void ConfigurationHandlerReturnsCorrectNonString()
        {
            string json = JsonConvert.SerializeObject(new TestConfiguration { TestVariable = "123" });
            WriteExecuteDelete(handler =>
            {
                Assert.Equal(123, handler.Get<int>("TestVariable"));
            }, json);
        }

        #endregion
    }
}