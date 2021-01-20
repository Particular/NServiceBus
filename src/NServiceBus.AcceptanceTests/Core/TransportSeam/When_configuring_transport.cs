namespace NServiceBus.AcceptanceTests.Core.TransportSeam
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;
    using Transport;

    public class When_configuring_transport : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_provide_transport_definition_to_features()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithFeature>()
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.IsNotNull(context.TransportDefinition);
        }

        class Context : ScenarioContext
        {
            public TransportDefinition TransportDefinition { get; set; }
        }

        class EndpointWithFeature : EndpointConfigurationBuilder
        {
            public EndpointWithFeature()
            {
                EndpointSetup<DefaultServer>(c => c.EnableFeature<FeatureAccessingInfrastructure>());
            }

            class FeatureAccessingInfrastructure : Feature
            {
                protected override void Setup(FeatureConfigurationContext context)
                {
                    var testContext = (Context)context.Settings.Get<ScenarioContext>();
                    testContext.TransportDefinition = context.Settings.Get<TransportDefinition>();
                }
            }
        }
    }


}