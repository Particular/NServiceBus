using NServiceBus.Config.ConfigurationSource;
using NUnit.Framework;
using Rhino.Mocks;

namespace NServiceBus.UnitTests.Configuration
{
    [TestFixture]
    public class When_users_override_the_configration_source
    {
        private IConfigurationSource userConfigurationSource;

        [SetUp]
        public void SetUp()
        {
            userConfigurationSource = MockRepository.GenerateStub<IConfigurationSource>();
            Configure.With()
                .CustomConfigurationSource(userConfigurationSource);
        }

        [Test]
        public void NService_bus_should_resolve_configuration_from_that_source()
        {
            Configure.GetConfigSection<TestConfigurationSection>();


            userConfigurationSource.AssertWasCalled(c => c.GetConfiguration<TestConfigurationSection>());
        }

    }

    public class UserConfigurationSource : IConfigurationSource
    {
        public T GetConfiguration<T>() where T : class
        {
            return new TestConfigurationSection() as T;
        }
    }

}