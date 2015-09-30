namespace NServiceBus.AcceptanceTests.Timeouts
{
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Features;
    using NUnit.Framework;

    public class When_endpoint_uses_outdated_sql_transport_with_dtc : NServiceBusAcceptanceTest
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

        public class Context : ScenarioContext { }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(config =>
                {
                    config.EnableFeature<TimeoutManager>();
                    config.Transactions().EnableDistributedTransactions();
                    config.UseTransport<OutdatedSqlServerTransport>();
                });
            }
        }

        public class OutdatedSqlServerTransport : TransportWithFakeQueues
        {
            // needs to contain SqlServer in the name
            public OutdatedSqlServerTransport()
            {
                HasSupportForDistributedTransactions = true;
            }
        }

        public class Initalizer : Feature
        {
            public Initalizer()
            {
                EnableByDefault();
            }

            protected override void Setup(FeatureConfigurationContext context)
            {
                context.Container.ConfigureComponent<UpdatedTimeoutPersister>(DependencyLifecycle.SingleInstance);
            }
        }
    }
}
