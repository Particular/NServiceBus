namespace NServiceBus.AcceptanceTests.Core.Feature
{
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;

    public class When_enabling_feature_not_scanned : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_enable_feature()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithoutAssemblyScanning>()
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.IsTrue(context.FeatureSetupCalled);
            Assert.IsTrue(context.FeatureStartupTaskCalled);
        }

        class Context : ScenarioContext
        {
            public bool FeatureSetupCalled { get; set; }
            public bool FeatureStartupTaskCalled { get; set; }
        }

        class EndpointWithoutAssemblyScanning : EndpointConfigurationBuilder
        {
            public EndpointWithoutAssemblyScanning()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.AssemblyScanner().ScanAssembliesInNestedDirectories = false;
                    c.AssemblyScanner().ScanAppDomainAssemblies = false;

                    c.EnableFeature<CustomFeature>();
                }).ExcludeType<CustomFeature>();
            }

            public class CustomFeature : Feature
            {
                public CustomFeature() => EnableByDefault();

                protected override void Setup(FeatureConfigurationContext context)
                {
                    var testContext = context.Settings.Get<ScenarioContext>() as Context;
                    testContext.FeatureSetupCalled = true;

                    context.RegisterStartupTask(sp => new CustomFeatureStartupTask(sp.GetRequiredService<Context>()));
                }

                class CustomFeatureStartupTask : FeatureStartupTask
                {
                    Context testContext;

                    public CustomFeatureStartupTask(Context testContext)
                    {
                        this.testContext = testContext;
                    }

                    protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
                    {
                        testContext.FeatureStartupTaskCalled = true;
                        return Task.CompletedTask;
                    }

                    protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
                }
            }
        }
    }
}