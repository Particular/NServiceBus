namespace NServiceBus.AcceptanceTests.Timeouts
{
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Settings;
    using NServiceBus.Transports;
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

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(config =>
                {
                    config.Configurer.ConfigureComponent<UpdatedTimeoutPersister>(DependencyLifecycle.SingleInstance);
                    config.UseTransport<OutdatedSqlServerTransport>();
                });
            }
        }

        public class Context : ScenarioContext
        {
        }

        public class OutdatedSqlServerTransport : TransportDefinition
        {
            // needs to contain SqlServer in the name
            public OutdatedSqlServerTransport()
            {
                HasSupportForDistributedTransactions = true;
            }
        }

        public class OutdatedSqlServerTransportConfiguration : IConfigureTransport<OutdatedSqlServerTransport>
        {
            public void Configure(Configure config)
            {
                var selectedTransportDefinition = new OutdatedSqlServerTransport();
                SettingsHolder.Set("NServiceBus.Transport.SelectedTransport", selectedTransportDefinition);
                config.Configurer.RegisterSingleton<TransportDefinition>(selectedTransportDefinition);
            }
        }
    }
}