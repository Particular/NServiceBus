namespace NServiceBus.AcceptanceTests.Config
{
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Config;
    using NServiceBus.Config.ConfigurationSource;
    using NUnit.Framework;

    public class When_a_config_override_is_found : NServiceBusAcceptanceTest
    {
        static Address CustomErrorQ = Address.Parse("MyErrorQ");

        [Test]
        public void Should_be_used_instead_of_pulling_the_settings_from_appconfig()
        {
            var context = Scenario.Define<Context>()
                    .WithEndpoint<ConfigOverrideEndpoint>().Done(c =>c.ErrorQueueUsedByTheEndpoint != null)
                    .Run();

            Assert.AreEqual(CustomErrorQ, context.ErrorQueueUsedByTheEndpoint, "The error queue should have been changed");

        }

        public class Context : ScenarioContext
        {
            public bool IsDone { get; set; }

            public Address ErrorQueueUsedByTheEndpoint { get; set; }
        }

        public class ConfigOverrideEndpoint : EndpointConfigurationBuilder
        {

            public ConfigOverrideEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class ErrorQueueExtractor:IWantToRunWhenBusStartsAndStops
            {
                Configure configure;
                Context context;

                public ErrorQueueExtractor(Configure configure, Context context)
                {
                    this.configure = configure;
                    this.context = context;
                }

                public void Start()
                {
                    context.ErrorQueueUsedByTheEndpoint = Address.Parse(configure.Settings.GetConfigSection<MessageForwardingInCaseOfFaultConfig>().ErrorQueue);
                }

                public void Stop()
                {
                }
            }

            public class ConfigErrorQueue : IProvideConfiguration<MessageForwardingInCaseOfFaultConfig>
            {
                public MessageForwardingInCaseOfFaultConfig GetConfiguration()
                {

                    return new MessageForwardingInCaseOfFaultConfig
                    {
                        ErrorQueue = CustomErrorQ.ToString()
                    };
                }
            }
        }
    }


}