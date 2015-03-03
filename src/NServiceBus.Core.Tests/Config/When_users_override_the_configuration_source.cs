namespace NServiceBus.Core.Tests.Config
{
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

            var builder = new BusConfiguration();
            builder.CustomConfigurationSource(userConfigurationSource);

            config = builder.BuildConfiguration();
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