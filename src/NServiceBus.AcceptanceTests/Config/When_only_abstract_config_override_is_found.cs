namespace NServiceBus.AcceptanceTests.Config
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NServiceBus.Config;
    using NServiceBus.Config.ConfigurationSource;
    using NUnit.Framework;

    public class When_only_abstract_config_override_is_found : NServiceBusAcceptanceTest
    {

        [Test]
        public async Task Should_not_invoke_it()
        {
            await Scenario.Define<Context>()
                    .WithEndpoint<ConfigOverrideEndpoint>().Done(c => c.EndpointsStarted)
                    .Run();
        }

        public class Context : ScenarioContext
        {
        }

        public class ConfigOverrideEndpoint : EndpointConfigurationBuilder
        {
            public ConfigOverrideEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }
            
            abstract class ConfigErrorQueue : IProvideConfiguration<MessageForwardingInCaseOfFaultConfig>
            {
                public MessageForwardingInCaseOfFaultConfig GetConfiguration()
                {
                    throw new NotImplementedException();
                }
            }
        }
    }


}