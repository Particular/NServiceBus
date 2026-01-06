namespace NServiceBus.AcceptanceTests.Core.TransportSeam;

using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Features;
using NUnit.Framework;
using Transport;

public class When_transport_provides_feature : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_setup_feature()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithFeature>()
            .Done(c => c.EndpointsStarted)
            .Run();

        Assert.That(context.TransportFeatureRan, Is.True);
    }

    class Context : ScenarioContext
    {
        public bool TransportFeatureRan { get; set; }
    }

    class EndpointWithFeature : EndpointConfigurationBuilder
    {
        public EndpointWithFeature() => EndpointSetup<DefaultServer>(c => c.UseTransport(new CustomAcceptanceTestingTransport()));
    }

    class CustomAcceptanceTestingTransport : AcceptanceTestingTransport
    {
        public CustomAcceptanceTestingTransport() => EnableEndpointFeature<FeatureAccessingInfrastructure>();

        class FeatureAccessingInfrastructure : Feature
        {
            protected override void Setup(FeatureConfigurationContext context)
            {
                var testContext = (Context)context.Settings.Get<ScenarioContext>();
                testContext.TransportFeatureRan = true;
            }
        }
    }
}