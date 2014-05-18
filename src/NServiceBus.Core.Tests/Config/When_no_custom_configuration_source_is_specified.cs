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
            var config = Configure.With(new Type[] { });

            var configSection = config.GetConfigSection<TestConfigurationSection>();

            Assert.AreEqual(configSection.TestSetting,"test");
        }
    }
}