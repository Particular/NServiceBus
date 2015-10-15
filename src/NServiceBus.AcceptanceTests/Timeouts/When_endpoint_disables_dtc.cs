namespace NServiceBus.AcceptanceTests.Timeouts
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTesting.Support;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Timeout.Core;
    using NUnit.Framework;

    class When_endpoint_disables_dtc : NServiceBusAcceptanceTest
    {
        [Test]
        public void Endpoint_should_not_start_with_msmq()
        {
            var context = new Context();

            var exception = Assert.Throws<AggregateException>(() => Scenario.Define(context)
                .WithEndpoint<MsmqEndpoint>()
                .Done(c => c.EndpointsStarted)
                .Run())
                .InnerException as ScenarioException;

            Assert.IsFalse(context.EndpointsStarted);
            StringAssert.Contains("You are using an outdated timeout dispatch implementation which can lead to message loss!", exception.Message);
        }

        [Test]
        public void Endpoint_should_start_with_msmq_when_warning_disabled()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<MsmqEndpoint>(c =>
                    c.CustomConfig(configure => configure.SuppressOutdatedTimeoutDispatchWarning()))
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.True(context.EndpointsStarted);
        }
        
        public class MsmqEndpoint : EndpointConfigurationBuilder
        {
            public MsmqEndpoint()
            {
                EndpointSetup<DefaultServer>(config =>
                {
                    config.UseTransport<Msmq>();
                    Configure.Transactions.Advanced(s => s.DisableDistributedTransactions());
                });
            }
        }

        public class Context : ScenarioContext
        {
        }
    }
}

