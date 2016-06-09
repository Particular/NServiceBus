namespace NServiceBus.AcceptanceTests.Basic
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    public class When_depending_on_untyped_feature : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_enable_when_untyped_dependency_enabled()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithFeatures>(b => b.CustomConfig(c =>
                {
                    c.EnableFeature<UntypedDependentFeature>();
                    c.EnableFeature<DependencyFeature>();
                }))
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.That(context.UntypedDependencyFeatureSetUp, Is.True);
        }

        [Test]
        public async Task Should_disable_when_untyped_dependency_disabled()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithFeatures>(b => b.CustomConfig(c =>
                {
                    c.DisableFeature<UntypedDependentFeature>();
                    c.EnableFeature<DependencyFeature>();
                }))
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.That(context.UntypedDependencyFeatureSetUp, Is.False);
        }

        class Context : ScenarioContext
        {
            public bool UntypedDependencyFeatureSetUp { get; set; }
        }

        public class EndpointWithFeatures : EndpointConfigurationBuilder
        {
            public EndpointWithFeatures()
            {
                EndpointSetup<DefaultServer>();
            }
        }

        public class UntypedDependentFeature : Feature
        {
            public UntypedDependentFeature()
            {
                var featureTypeFullName = typeof(DependencyFeature).FullName;
                DependsOn(featureTypeFullName);
            }

            protected override void Setup(FeatureConfigurationContext context)
            {
                var testContext = (Context) context.Settings.Get<ScenarioContext>();
                testContext.UntypedDependencyFeatureSetUp = true;
            }
        }

        public class DependencyFeature : Feature
        {
            protected override void Setup(FeatureConfigurationContext context)
            {
            }
        }
    }
}