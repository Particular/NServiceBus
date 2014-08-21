namespace NServiceBus.Core.Tests.Config
{
    using NUnit.Framework;

    [TestFixture]
    public class When_no_custom_configuration_source_is_specified
    {
        [Test]
        public void The_default_configuration_source_should_be_default()
        {
            var config = new BusConfiguration().BuildConfiguration();

            
            Assert.AreEqual(config.Settings.GetConfigSection<TestConfigurationSection>().TestSetting, "test");
        }
    }
}