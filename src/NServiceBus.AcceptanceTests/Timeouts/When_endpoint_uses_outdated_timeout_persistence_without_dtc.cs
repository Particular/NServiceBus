namespace NServiceBus.AcceptanceTests.Timeouts
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTesting.Support;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Settings;
    using NServiceBus.Transports;
    using NUnit.Framework;

    public class When_endpoint_uses_outdated_timeout_persistence_without_dtc : NServiceBusAcceptanceTest
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
            StringAssert.Contains("You are using an outdated timeout persistence which can lead to message loss!", scenarioException.InnerException.Message);
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(config =>
                {
                    config.UseTransport<NonDtcTransportDefinition>();
                    config.Configurer.ConfigureComponent<OutdatedTimeoutPersister>(DependencyLifecycle.SingleInstance);
                });
            }
        }

        public class Context : ScenarioContext
        {
        }

        class NonDtcTransportDefinition : TransportDefinition
        {
            public NonDtcTransportDefinition()
            {
                HasSupportForDistributedTransactions = false;
            }
        }

        class NonDtcTransport : IConfigureTransport<NonDtcTransportDefinition>
        {
            public void Configure(Configure config)
            {
                var selectedTransportDefinition = Activator.CreateInstance<NonDtcTransportDefinition>();
                SettingsHolder.Set("NServiceBus.Transport.SelectedTransport", selectedTransportDefinition);
                config.Configurer.RegisterSingleton<TransportDefinition>(selectedTransportDefinition);
            }
        } 
    }
}