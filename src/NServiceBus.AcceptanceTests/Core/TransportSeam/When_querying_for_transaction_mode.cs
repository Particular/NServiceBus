namespace NServiceBus.AcceptanceTests.Core.TransportSeam
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using FakeTransport;
    using Features;
    using NUnit.Framework;

    public class When_querying_for_transaction_mode : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_retrieve_value_set_for_transport_transaction_mode()
        {
            await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(c => c.CustomConfig(ec =>
                {
                    ec.EnableFeature<Endpoint.FeatureEnabledByUser>();
                    var transport = ec.UseTransport<LearningTransport>();
                    transport.Transactions(TransportTransactionMode.ReceiveOnly);
                }))
                .Done(c => c.EndpointsStarted)
                .Run();
        }

        class Context : ScenarioContext
        {
        }

        class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class FeatureEnabledByUser : Feature
            {
                protected override void Setup(FeatureConfigurationContext context)
                {
                    var endpointTransactionMode = ConsistencyGuarantees.TransactionModeSettingsExtensions.GetRequiredTransactionModeForReceives(context.Settings);
                    Assert.AreEqual(TransportTransactionMode.ReceiveOnly, endpointTransactionMode, "Transport transaction mode for the endpoint did not match the expected value.");
                }
            }
        }
    }
}
