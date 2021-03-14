namespace NServiceBus.AcceptanceTests.Core.TransportSeam
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using ConsistencyGuarantees;
    using NUnit.Framework;
    using System.Threading;

    public class When_querying_for_transaction_mode : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_retrieve_value_set_for_transport_transaction_mode()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>()
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.AreEqual(TransportTransactionMode.ReceiveOnly, context.TransactionModeFromSettingsExtensions, "Transport transaction mode for the endpoint did not match the expected value.");
        }

        class Context : ScenarioContext
        {
            public TransportTransactionMode TransactionModeFromSettingsExtensions { get; set; }
        }

        class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.EnableFeature<FeatureEnabledByUser>();
                    c.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly;
                });
            }

            public class FeatureEnabledByUser : Feature
            {
                public Context TestContext { get; set; }

                protected override Task Setup(FeatureConfigurationContext context, CancellationToken cancellationToken = default)
                {
                    context.Settings.Get<Context>().TransactionModeFromSettingsExtensions = context.Settings.GetRequiredTransactionModeForReceives();
                    return Task.CompletedTask;
                }
            }
        }
    }
}
