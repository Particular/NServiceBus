namespace NServiceBus.Hosting.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Configuration;
    using NUnit.Framework;

    [TestFixture]
    public class ConfigManagerTests
    {
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

        [Test]
        public void ExcludeIConfigureThisEndpoint()
        {
            var configManager = new ConfigManager(new List<Assembly> {typeof (ConfigManagerTests).Assembly}, null);
            Assert.IsTrue(!configManager.toInitialize.All(x => typeof (IConfigureThisEndpoint).IsAssignableFrom(x)));
        }

        [Test]
        public void ShouldAllImplementInterface()
        {
            var configManager = new ConfigManager(new List<Assembly> {typeof (ConfigManagerTests).Assembly}, null);
            Assert.IsTrue(configManager.toInitialize.All(x => typeof (IWantCustomInitialization).IsAssignableFrom(x)));
        }

        [Test]
        public void ShouldNotContainAbstractClasses()
        {
            var configManager = new ConfigManager(new List<Assembly> {typeof (ConfigManagerTests).Assembly}, null);
            Assert.AreEqual(0, configManager.toInitialize.Count(x => x.IsAbstract));
        }

        [Test]
        public void ShouldNotContainInterfaces()
        {
            var configManager = new ConfigManager(new List<Assembly> {typeof (ConfigManagerTests).Assembly}, null);
            Assert.AreEqual(0, configManager.toInitialize.Count(x => x.IsInterface));
        }
    }
}