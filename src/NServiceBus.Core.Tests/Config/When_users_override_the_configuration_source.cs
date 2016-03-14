namespace NServiceBus.Core.Tests.Config
{
    using System.Threading.Tasks;
    using NServiceBus.Config.ConfigurationSource;
    using NServiceBus.Features;
    using NServiceBus.Settings;
    using NUnit.Framework;

    [TestFixture]
    public class When_users_override_the_configuration_source
    {
        [Test]
        public async Task NService_bus_should_resolve_configuration_from_that_source()
        {
            var builder = new EndpointConfiguration("myendpoint");

            builder.SendOnly();
            builder.TypesToScanInternal(new[] { typeof(ConfigSectionValidatorFeature) });
            builder.EnableFeature<ConfigSectionValidatorFeature>();
            builder.CustomConfigurationSource(new UserConfigurationSource());

            var endpoint = await Endpoint.Start(builder);
            await endpoint.Stop();
        }

        class ConfigSectionValidatorFeature : Feature
        {
            protected internal override void Setup(FeatureConfigurationContext context)
            {
                context.RegisterStartupTask(new ValidatorTask(context.Settings));
            }

            class ValidatorTask : FeatureStartupTask
            {
                ReadOnlySettings settings;

                public ValidatorTask(ReadOnlySettings settings)
                {
                    this.settings = settings;
                }

                protected override Task OnStart(IMessageSession session)
                {
                    var section = settings.GetConfigSection<TestConfigurationSection>();
                    Assert.AreEqual(section.TestSetting, "TestValue");
                    return TaskEx.CompletedTask;
                }

                protected override Task OnStop(IMessageSession session)
                {
                    return TaskEx.CompletedTask;
                }
            }
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