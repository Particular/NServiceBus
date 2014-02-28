namespace NServiceBus.AcceptanceTests.Configuration
{
    using Config;
    using Config.ConfigurationSource;
    using EndpointTemplates;
    using AcceptanceTesting;
    using Faults.Forwarder;
    using NUnit.Framework;
    using Unicast;
    using Unicast.Transport;

    public class When_a_config_override_is_found : NServiceBusAcceptanceTest
    {
        static Address CustomErrorQ = Address.Parse("MyErrorQ");

        [Test]
        public void Should_be_used_instead_of_pulling_the_settings_from_appconfig()
        {
            var context = Scenario.Define<Context>()
                    .WithEndpoint<ConfigOverrideEndpoint>(b => b.When(c => c.EndpointsStarted, (bus, cc) =>
                        {
                            var unicastBus = (UnicastBus)bus;
                            var transport = (TransportReceiver)unicastBus.Transport;
                            var fm = (FaultManager)transport.FailureManager;

                            cc.ErrorQueueUsedByTheEndpoint = fm.ErrorQueue;
                            cc.IsDone = true;
                        }))
                    .Done(c => c.IsDone)
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
                EndpointSetup<DefaultServer>(c => c.MessageForwardingInCaseOfFault());
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