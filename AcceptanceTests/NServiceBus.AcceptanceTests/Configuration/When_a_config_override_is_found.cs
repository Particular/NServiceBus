namespace NServiceBus.AcceptanceTests.Configuration
{
    using Config;
    using Config.ConfigurationSource;
    using EndpointTemplates;
    using AcceptanceTesting;
    using Faults.Forwarder;
    using NUnit.Framework;
    using ScenarioDescriptors;
    using Unicast;
    using Unicast.Transport;

    public class When_a_config_override_is_found : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_be_used_instead_of_pulling_the_settings_from_appconfig()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<ConfigOverrideEndpoint>(b => b.When(c => c.EndpointsStarted, (bus, context) =>
                        {
                            var unicastBus = (UnicastBus) bus;
                            var transport = (TransportReceiver) unicastBus.Transport;
                            var fm = (FaultManager) transport.FailureManager;

                            context.IsDone = fm.ErrorQueue == Address.Parse("MyErrorQ");
                        }))
                    .Done(c => c.IsDone)
                    .Repeat(r => r.For(Transports.Msmq))
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public bool IsDone{ get; set; }
        }

        public class ConfigOverrideEndpoint : EndpointConfigurationBuilder
        {
            public ConfigOverrideEndpoint()
            {
                EndpointSetup<DefaultServer>(c=>c.MessageForwardingInCaseOfFault());
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