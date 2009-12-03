using NServiceBus.Config.ConfigurationSource;
using NUnit.Framework;
using NBehave.Spec.NUnit;

namespace NServiceBus.Config.UnitTests
{
    [TestFixture]
    public class When_users_override_the_configuration_source
    {
        private IConfigurationSource userConfigurationSource;

        [SetUp]
        public void SetUp()
        {
            this.userConfigurationSource = new UserConfigurationSource();
            Configure.With()
                .CustomConfigurationSource(this.userConfigurationSource);
        }
        
        [Test]
        public void NService_bus_should_resolve_configuration_from_that_source()
        {
            var section = Configure.GetConfigSection<TestConfigurationSection>();


            section.TestSetting.ShouldEqual("TestValue");
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