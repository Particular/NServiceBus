namespace NServiceBus.Core.Tests.Config
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    public class When_no_custom_configuration_source_is_specified
    {

        [SetUp]
        public void SetUp()
        {
            Configure.With(new Type[]{});
        }

        [Test]
        public void The_default_configuration_source_should_be_default()
        {
            var configSection = Configure.GetConfigSection<TestConfigurationSection>();

            Assert.AreEqual(configSection.TestSetting,"test");
        }
    }
}