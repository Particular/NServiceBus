using NBehave.Spec.NUnit;
using NUnit.Framework;
using Rhino.Mocks;

namespace NServiceBus.Integration.Azure.Tests
{
    [TestFixture]
    public class When_using_the_azure_configuration_source
    {
        [Test]
        public void The_service_configuration_should_override_appconfig()
        {
            var azureSettings = MockRepository.GenerateStub<IAzureConfigurationSettings>();

            azureSettings.Stub(x => x.GetSetting("TestConfigSection.StringSetting")).Return("test");
            var configSource = new AzureConfigurationSource(azureSettings);

            configSource.GetConfiguration<TestConfigSection>().StringSetting.ShouldEqual("test");
        }

       
    }
} 