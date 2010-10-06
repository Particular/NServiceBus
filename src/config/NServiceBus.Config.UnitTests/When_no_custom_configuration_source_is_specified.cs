using System;
using NUnit.Framework;

namespace NServiceBus.Config.UnitTests
{
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

        [Test]
        public void Getting_sections_that_not_inherits_from_configsection_should_fail()
        {
            Assert.Throws<ArgumentException>(() => Configure.GetConfigSection<IllegalSection>());
        }
    }

    public class IllegalSection
    {
    }
}