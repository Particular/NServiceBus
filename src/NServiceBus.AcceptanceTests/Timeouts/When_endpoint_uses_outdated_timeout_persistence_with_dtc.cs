namespace NServiceBus.AcceptanceTests.Timeouts
{
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_endpoint_uses_outdated_timeout_persistence_with_dtc : NServiceBusAcceptanceTest
    {
        [Test]
        public void Endpoint_should_start()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<Endpoint>()
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.IsTrue(context.EndpointsStarted);
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(config =>
                {
                    config.Configurer.ConfigureComponent<OutdatedTimeoutPersister>(DependencyLifecycle.SingleInstance);
                    Configure.Transactions.Advanced(s => s.EnableDistributedTransactions());
                });
            }
        }

        public class Context : ScenarioContext
        {
        } 
    }
}