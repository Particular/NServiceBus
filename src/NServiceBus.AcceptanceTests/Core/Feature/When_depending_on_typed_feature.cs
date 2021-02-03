namespace NServiceBus.AcceptanceTests.Core.Feature
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    public class When_depending_on_typed_feature : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_enable_when_typed_dependency_enabled()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithFeatures>(b => b.CustomConfig(c =>
                {
                    c.EnableFeature<TypedDependentFeature>();
                    c.EnableFeature<DependencyFeature>();
                }))
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.That(context.TypedDependencyFeatureSetUp, Is.True);
        }

        [Test]
        public async Task Should_disable_when_typed_dependency_disabled()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithFeatures>(b => b.CustomConfig(c =>
                {
                    c.DisableFeature<TypedDependentFeature>();
                    c.EnableFeature<DependencyFeature>();
                }))
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.That(context.TypedDependencyFeatureSetUp, Is.False);
        }

        class Context : ScenarioContext
        {
            public bool TypedDependencyFeatureSetUp { get; set; }
        }

        public class EndpointWithFeatures : EndpointConfigurationBuilder
        {
            public EndpointWithFeatures()
            {
                EndpointSetup<DefaultServer>();
            }
        }

        public class TypedDependentFeature : Feature
        {
            public TypedDependentFeature()
            {
                DependsOn<DependencyFeature>();
            }

            protected override void Setup(FeatureConfigurationContext context)
            {
                var testContext = (Context)context.Settings.Get<ScenarioContext>();
                testContext.TypedDependencyFeatureSetUp = true;
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