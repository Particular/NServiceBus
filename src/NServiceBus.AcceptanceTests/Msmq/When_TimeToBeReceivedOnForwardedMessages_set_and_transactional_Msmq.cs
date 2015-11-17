namespace NServiceBus.AcceptanceTests.Msmq
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTesting.Support;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NServiceBus.Config;
    using NUnit.Framework;

    public class When_TimeToBeReceivedOnForwardedMessages_set_and_transactional_Msmq : NServiceBusAcceptanceTest
    {
        [Test]
        public void Endpoint_should_not_start_and_show_error()
        {
            var context = new Context();
            var scenarioException = Assert.Throws<AggregateException>(() => Scenario.Define(context)
                .WithEndpoint<Endpoint>()
                .Done(c => c.EndpointsStarted)
                .Repeat(r => r.For<MsmqOnly>())
                .Run())
                .InnerException as ScenarioException;

            Assert.IsFalse(context.EndpointsStarted);
            Assert.IsNotNull(scenarioException);
            StringAssert.Contains("Setting a custom TimeToBeReceivedOnForwardedMessages is not supported on transactional MSMQ.", scenarioException.InnerException.Message);
        }

        public class Context : ScenarioContext { }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(config =>
                {
                    config.Transactions().Enable();
                })
                .WithConfig<UnicastBusConfig>(c => c.TimeToBeReceivedOnForwardedMessages = TimeSpan.FromHours(1));
            }
        }
    }
}
