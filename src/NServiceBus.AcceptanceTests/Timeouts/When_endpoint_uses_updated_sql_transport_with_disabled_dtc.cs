namespace NServiceBus.AcceptanceTests.Timeouts
{
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Settings;
    using NServiceBus.Transports;
    using NUnit.Framework;

    public class When_endpoint_uses_updated_sql_transport_with_disabled_dtc : NServiceBusAcceptanceTest
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
                    config.Configurer.ConfigureComponent<UpdatedTimeoutPersister>(DependencyLifecycle.SingleInstance);
                    config.UseTransport<UpdatedSqlServerTransport>();
                    Configure.Transactions.Advanced(s => s.DisableDistributedTransactions());
                });
            }
        }

        public class Context : ScenarioContext
        {
        }

        public class UpdatedSqlServerTransport : TransportDefinition
        {
            // needs to contain SqlServer in the name
            public UpdatedSqlServerTransport()
            {
                HasSupportForDistributedTransactions = true;
                SettingsHolder.Set("NServiceBus.Transport.SupportsNativeTransactionSuppression", true);
            }
        }

        public class UpdatedSqlServerTransportConfiguration : IConfigureTransport<UpdatedSqlServerTransport>
        {
            public void Configure(Configure config)
            {
                var selectedTransportDefinition = new UpdatedSqlServerTransport();
                SettingsHolder.Set("NServiceBus.Transport.SelectedTransport", selectedTransportDefinition);
                config.Configurer.RegisterSingleton<TransportDefinition>(selectedTransportDefinition);
            }
        }
    }
}