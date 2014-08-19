namespace NServiceBus.Core.Tests.Config
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    public class When_no_custom_configuration_source_is_specified
    {
        [Test]
        public void The_default_configuration_source_should_be_default()
        {
            var builder = new ConfigurationBuilder();

            builder.TypesToScan(new Type[]{});

            var config = Configure.With(builder);

            var configSection = config.Settings.GetConfigSection<TestConfigurationSection>();

            Assert.AreEqual(configSection.TestSetting, "test");
        }
    }
}