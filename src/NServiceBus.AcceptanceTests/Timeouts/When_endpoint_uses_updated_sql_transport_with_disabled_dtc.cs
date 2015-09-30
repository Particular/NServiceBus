namespace NServiceBus.AcceptanceTests.Timeouts
{
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Features;
    using NUnit.Framework;

    class When_endpoint_uses_updated_sql_transport_with_disabled_dtc : NServiceBusAcceptanceTest
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
                    config.Transactions().DisableDistributedTransactions();
                    config.UseTransport<UpdatedSqlServerTransport>();
                });
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

        public class UpdatedSqlServerTransport : TransportWithFakeQueues
        {
            // needs to contain SqlServer in the name
            public UpdatedSqlServerTransport()
            {
                HasSupportForDistributedTransactions = true;
            }

            public class AddNewSqlServerTransportSetting : IWantToRunBeforeConfigurationIsFinalized
            {
                public void Run(Configure config)
                {
                    config.Settings.Set("NServiceBus.Transport.SupportsNativeTransactionSuppression", true);
                }
            }
        }
    }
}
