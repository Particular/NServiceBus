namespace NServiceBus.AcceptanceTests.Core.Feature;

using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Features;
using NUnit.Framework;

public class When_features_are_scanned : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_run_features()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithFeature>()
            .Done(c => c.EndpointsStarted)
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.RootFeatureCalled, Is.True);
            Assert.That(context.DependentFeatureCalled, Is.True);
        }
    }

    class Context : ScenarioContext
    {
        public bool RootFeatureCalled { get; set; }
        public bool DependentFeatureCalled { get; set; }
    }

    class EndpointWithFeature : EndpointConfigurationBuilder
    {
        public EndpointWithFeature() =>
            EndpointSetup<DefaultServer>()
                .IncludeType<FeatureDiscoveredByScanning>(); //simulate that the feature is included in scanning

        sealed class FeatureDiscoveredByScanning : Feature
        {
            FeatureDiscoveredByScanning()
            {
#pragma warning disable CS0618 // Type or member is obsolete
                EnableByDefault();
#pragma warning restore CS0618 // Type or member is obsolete
                EnableByDefault<DependentFeature>();

                DependsOn<DependentFeature>();
            }

            protected override void Setup(FeatureConfigurationContext context) => context.Settings.Get<Context>().RootFeatureCalled = true;
        }

        sealed class DependentFeature : Feature
        {
            DependentFeature() { }
            protected override void Setup(FeatureConfigurationContext context) => context.Settings.Get<Context>().DependentFeatureCalled = true;
        }
    }
}