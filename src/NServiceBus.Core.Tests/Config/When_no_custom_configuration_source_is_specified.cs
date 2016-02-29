namespace NServiceBus.Core.Tests.Config
{
    using System.Threading.Tasks;
    using NServiceBus.Features;
    using NServiceBus.Settings;
    using NServiceBus.Transports.Msmq;
    using NUnit.Framework;

    [TestFixture]
    public class When_no_custom_configuration_source_is_specified
    {
        [Test]
        public async Task The_default_configuration_source_should_be_default()
        {
            var config = new EndpointConfiguration();

            config.SendOnly();
            config.UseTransport<MsmqTransport>().UseSubscriptionStore<FakeSubscriptionStore>();
            config.TypesToScanInternal(new[] { typeof(ConfigSectionValidatorFeature) });
            config.EnableFeature<ConfigSectionValidatorFeature>();

            var endpoint = await Endpoint.Start(config);
            await endpoint.Stop();
        }

        class FakeSubscriptionStore : SubscriptionStoreDefinition
        {
            protected internal override SubscriptionStoreInfrastructure Initialize(SettingsHolder settings)
            {
                return new SubscriptionStoreInfrastructure(() => null, () => null);
            }
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

                protected override Task OnStart(IMessageSession session)
                {
                    Assert.AreEqual(settings.GetConfigSection<TestConfigurationSection>().TestSetting, "test");
                    return TaskEx.CompletedTask;
                }

                protected override Task OnStop(IMessageSession session)
                {
                    return TaskEx.CompletedTask;
                }
            }
        }
    }
}