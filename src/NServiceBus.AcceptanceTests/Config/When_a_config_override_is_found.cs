namespace NServiceBus.AcceptanceTests.Config
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NServiceBus.Config;
    using NServiceBus.Config.ConfigurationSource;
    using NServiceBus.Settings;
    using NUnit.Framework;

    public class When_a_config_override_is_found : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_be_used_instead_of_pulling_the_settings_from_appconfig()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<ConfigOverrideEndpoint>().Done(c => c.ErrorQueueUsedByTheEndpoint != null)
                .Run();

            Assert.AreEqual("MyErrorQ", context.ErrorQueueUsedByTheEndpoint, "The error queue should have been changed");
        }

        public class Context : ScenarioContext
        {
            public bool IsDone { get; set; }

            public string ErrorQueueUsedByTheEndpoint { get; set; }
        }

        public class ConfigOverrideEndpoint : EndpointConfigurationBuilder
        {
            public ConfigOverrideEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class ErrorQueueExtractor : IWantToRunWhenBusStartsAndStops
            {
                ReadOnlySettings settings;
                Context context;

                public ErrorQueueExtractor(ReadOnlySettings settings, Context context)
                {
                    this.settings = settings;
                    this.context = context;
                }

                public Task StartAsync(ISendOnlyBus bus)
                {
                    context.ErrorQueueUsedByTheEndpoint = settings.GetConfigSection<MessageForwardingInCaseOfFaultConfig>().ErrorQueue;
                    return Task.FromResult(0);
                }

                public Task StopAsync()
                {
                    return Task.FromResult(0);
                }
            }

            public class ConfigErrorQueue : IProvideConfiguration<MessageForwardingInCaseOfFaultConfig>
            {
                public MessageForwardingInCaseOfFaultConfig GetConfiguration()
                {
                    return new MessageForwardingInCaseOfFaultConfig
                    {
                        ErrorQueue = "MyErrorQ"
                    };
                }
            }
        }
    }
}