namespace NServiceBus.Core.Tests.Config
{
    using System;
    using NServiceBus.Config.ConfigurationSource;
    using NUnit.Framework;

    [TestFixture]
    public class When_users_override_the_configuration_source
    {
        IConfigurationSource userConfigurationSource;

        Configure config;

        [SetUp]
        public void SetUp()
        {
            userConfigurationSource = new UserConfigurationSource();
            config = Configure.With(new Type[] { })
                .CustomConfigurationSource(userConfigurationSource);
        }

        [TearDown]
        public void TearDown()
        {
            Configure.Instance.CustomConfigurationSource(new DefaultConfigurationSource());
        }
        
        [Test]
        public void NService_bus_should_resolve_configuration_from_that_source()
        {
            var section = config.Settings.GetConfigSection<TestConfigurationSection>();

            Assert.AreEqual(section.TestSetting,"TestValue");
        }

    }

    public class UserConfigurationSource : IConfigurationSource
    {
        T IConfigurationSource.GetConfiguration<T>()
        {
            var section = new TestConfigurationSection {TestSetting = "TestValue"};

            return section as T;
        }
    }
}