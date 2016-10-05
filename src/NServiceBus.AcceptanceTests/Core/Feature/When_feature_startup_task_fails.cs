namespace NServiceBus.AcceptanceTests.Core.Feature
{
    using System;
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
            var exception = Assert.ThrowsAsync<AggregateException>(() =>
                Scenario.Define<ScenarioContext>()
                    .WithEndpoint<EndpointWithStartupTask>()
                    .Run());

            Assert.That(exception.InnerException.InnerException, Is.TypeOf<SimulatedException>());
        }

        class EndpointWithStartupTask : EndpointConfigurationBuilder
        {
            public EndpointWithStartupTask()
            {
                EndpointSetup<DefaultServer>(c => c.EnableFeature<FeatureWithStartupTask>());
            }

            class FeatureWithStartupTask : Feature
            {
                protected override void Setup(FeatureConfigurationContext context)
                {
                    context.RegisterStartupTask(new FailingStartupTask());
                }
            }

            class FailingStartupTask : FeatureStartupTask
            {
                protected override Task OnStart(IMessageSession session)
                {
                    throw new SimulatedException();
                }

                protected override Task OnStop(IMessageSession session)
                {
                    return Task.FromResult(0);
                }
            }
        }
    }
}