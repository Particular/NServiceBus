namespace NServiceBus.AcceptanceTests.Timeouts
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTesting.Support;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Settings;
    using NServiceBus.Timeout.Core;
    using NServiceBus.Transports;
    using NUnit.Framework;

    public class When_endpoint_uses_outdated_sql_transport_with_disabled_dtc : NServiceBusAcceptanceTest
    {
        [Test]
        public void Endpoint_should_not_start_and_show_warning()
        {
            var context = new Context();

            var scenarioException = Assert.Throws<AggregateException>(() => Scenario.Define(context)
                    .WithEndpoint<Endpoint>()
                    .Done(c => c.EndpointsStarted)
                    .Run())
                    .InnerException as ScenarioException;

            Assert.IsFalse(context.EndpointsStarted);
            StringAssert.Contains("You are using an outdated transport which can lead to message loss!", scenarioException.InnerException.Message);
        }

        [Test]
        public void Endpoint_should_start_when_warning_suppressed()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<Endpoint>(c => c
                    .CustomConfig(configure => configure.SuppressOutdatedTransportWarning()))
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
                    Configure.Transactions.Advanced(s => s.DisableDistributedTransactions());
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