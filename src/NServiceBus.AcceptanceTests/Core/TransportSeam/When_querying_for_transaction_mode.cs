namespace NServiceBus.AcceptanceTests.Core.TransportSeam
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NServiceBus.ConsistencyGuarantees;
    using NUnit.Framework;

    public class When_querying_for_transaction_mode : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_retrieve_value_set_for_transport_transaction_mode()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(c => c.CustomConfig(ec =>
                {
                    ec.EnableFeature<Endpoint.FeatureEnabledByUser>();
                    var transport = ec.UseTransport<LearningTransport>();
                    transport.Transactions(TransportTransactionMode.ReceiveOnly);
                }))
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
                EndpointSetup<DefaultServer>();
            }

            public class FeatureEnabledByUser : Feature
            {
                public Context TestContext { get; set; }

                protected override void Setup(FeatureConfigurationContext context)
                {
                    context.Settings.Get<Context>().TransactionModeFromSettingsExtensions = context.Settings.GetRequiredTransactionModeForReceives();
                }
            }
        }
    }
}
