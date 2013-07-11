using NServiceBus.Config.ConfigurationSource;
using NUnit.Framework;
using Rhino.Mocks;

namespace NServiceBus.Integration.Azure.Tests
{
    [TestFixture]
    [Category("Azure")]
    public class When_using_the_azure_configuration_source
    {
        private IAzureConfigurationSettings azureSettings;
        private IConfigurationSource configSource;

        [SetUp]
        public void SetUp()
        {
            azureSettings = MockRepository.GenerateStub<IAzureConfigurationSettings>();

            configSource = new AzureConfigurationSource(azureSettings);
        }

        [Test]
        public void The_service_configuration_should_override_appconfig()
        {   
            azureSettings.Stub(x => x.GetSetting("TestConfigSection.StringSetting")).Return("test");

            Assert.AreEqual(configSource.GetConfiguration<TestConfigSection>().StringSetting,"test");
        }

        [Test]
        public void Overrides_should_be_possible_for_non_existing_sections()
        {
            azureSettings.Stub(x => x.GetSetting("SectionNotPresentInConfig.SomeSetting")).Return("test");

            Assert.AreEqual(configSource.GetConfiguration<SectionNotPresentInConfig>().SomeSetting,"test");
        }

        [Test]
        public void No_section_should_be_returned_if_both_azure_and_app_configs_are_empty()
        {
            Assert.Null(configSource.GetConfiguration<SectionNotPresentInConfig>());
        }

        [Test]
        public void Value_types_should_be_converted_from_string_to_its_native_type()
        {
            azureSettings.Stub(x => x.GetSetting("TestConfigSection.IntSetting")).Return("23");

            Assert.AreEqual(configSource.GetConfiguration<TestConfigSection>().IntSetting,23);
        }
    }
} 