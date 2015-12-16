namespace NServiceBus.Core.Tests.Config
{
    using System.Threading.Tasks;
    using NServiceBus.Features;
    using NServiceBus.Routing;
    using NServiceBus.Settings;
    using NUnit.Framework;

    [TestFixture]
    public class When_no_custom_configuration_source_is_specified
    {
        [Test]
        public async Task The_default_configuration_source_should_be_default()
        {
            var config = new BusConfiguration();

            config.SendOnly();
            config.TypesToScanInternal(new[] { typeof(ConfigSectionValidatorFeature) });
            config.EnableFeature<ConfigSectionValidatorFeature>();

            var endpoint = await Endpoint.Start(config);
            await endpoint.Stop();
        }

        class ConfigSectionValidatorFeature : Feature
        {
            protected internal override void Setup(FeatureConfigurationContext context)
            {
                context.RegisterStartupTask(() => new ValidatorTask(context.Settings));
            }

            class ValidatorTask : FeatureStartupTask
            {
                ReadOnlySettings settings;

                public ValidatorTask(ReadOnlySettings settings)
                {
                    this.settings = settings;
                }

                protected override Task OnStart(IBusSession session)
                {
                    Assert.AreEqual(settings.GetConfigSection<TestConfigurationSection>().TestSetting, "test");
                    return Task.FromResult(0);
                }

                protected override Task OnStop(IBusSession session)
                {
                    return TaskEx.Completed;
                }
            }
        }
    }
}