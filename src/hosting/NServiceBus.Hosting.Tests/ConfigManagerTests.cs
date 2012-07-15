using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NServiceBus.Hosting.Configuration;
using NUnit.Framework;

namespace NServiceBus.Hosting.Tests
{
    [TestFixture]
    public class ConfigManagerTests
    {

        [Test]
        public void ShouldNotContainInerfaces()
        {
            var configManager = new ConfigManager(new List<Assembly> {typeof (ConfigManagerTests).Assembly}, null);
            Assert.AreEqual(0, configManager.toInitialize.Count(x => x.IsInterface));
        }
        [Test]
        public void ShouldNotContainAbstractClasses()
        {
            var configManager = new ConfigManager(new List<Assembly> { typeof(ConfigManagerTests).Assembly }, null);
            Assert.AreEqual(0, configManager.toInitialize.Count(x => x.IsAbstract));
        }
        [Test]
        public void ShouldAllImplementInterface()
        {
            var configManager = new ConfigManager(new List<Assembly> { typeof(ConfigManagerTests).Assembly }, null);
            Assert.IsTrue(configManager.toInitialize.All(x => typeof(IWantCustomInitialization).IsAssignableFrom(x)));
        }
        [Test]
        public void ExcludeIConfigureThisEndpoint()
        {
            var configManager = new ConfigManager(new List<Assembly> { typeof(ConfigManagerTests).Assembly }, null);
            Assert.IsTrue(!configManager.toInitialize.All(x => typeof(IConfigureThisEndpoint).IsAssignableFrom(x)));
        }

        public interface IWantCustomInitializationChild : IWantCustomInitialization
        {
             
        }
        public class WantCustomInitializationChild : IWantCustomInitializationChild
        {
            public void Init()
            {
            }
        }
        public class ClassNotImplementing
        {
        }
        public class ConfigureThisEndpoint : IConfigureThisEndpoint
        {
        }
        public class WantCustomInitialization : IWantCustomInitialization
        {
            public void Init()
            {
            }
        }
        public abstract class AbstractWantCustomInitialization : IWantCustomInitialization
        {
            public void Init()
            {
            }
        }
        public class ConcreteWantCustomInitialization : AbstractWantCustomInitialization
        {
        }
    }
}