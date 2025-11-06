namespace NServiceBus.AcceptanceTests.Core.Feature;

using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Features;
using NUnit.Framework;

public class When_feature_startup_task_fails : NServiceBusAcceptanceTest
{
    [Test]
    public void Should_not_start_endpoint()
    {
        Assert.ThrowsAsync<SimulatedException>(() =>
            Scenario.Define<ScenarioContext>()
                .WithEndpoint<EndpointWithStartupTask>()
                .Run());
    }

    class EndpointWithStartupTask : EndpointConfigurationBuilder
    {
        public EndpointWithStartupTask()
        {
            EndpointSetup<DefaultServer>(c => c.EnableFeature<FeatureWithStartupTask>());
        }

        class FeatureWithStartupTask : Feature, IFeatureFactory
        {
            protected override void Setup(FeatureConfigurationContext context)
            {
                context.RegisterStartupTask(new FailingStartupTask());
            }

            static Feature IFeatureFactory.Create() => new FeatureWithStartupTask();
        }

        class FailingStartupTask : FeatureStartupTask
        {
            protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
            {
                throw new SimulatedException();
            }

            protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }
    }
}