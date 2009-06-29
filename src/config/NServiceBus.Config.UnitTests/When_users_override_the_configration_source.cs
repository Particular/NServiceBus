using NServiceBus.Config.ConfigurationSource;
using NUnit.Framework;
using Rhino.Mocks;

namespace NServiceBus.Config.UnitTests
{
    [TestFixture]
    public class When_users_override_the_configration_source
    {
        private IConfigurationSource userConfigurationSource;

        [SetUp]
        public void SetUp()
        {
            this.userConfigurationSource = MockRepository.GenerateStub<IConfigurationSource>();
            Configure.With()
                .CustomConfigurationSource(this.userConfigurationSource);
        }

        [Test]
        public void NService_bus_should_resolve_configuration_from_that_source()
        {
            Configure.GetConfigSection<TestConfigurationSection>();


            this.userConfigurationSource.AssertWasCalled(c => c.GetConfiguration<TestConfigurationSection>());
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