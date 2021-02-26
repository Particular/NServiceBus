namespace NServiceBus.AcceptanceTests.Core.Stopping
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using FakeTransport;
    using Features;
    using Logging;
    using NUnit.Framework;

    public class When_feature_startup_task_throws_on_stop : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_log_exception()
        {
            var context = await Scenario.Define<ScenarioContext>()
                .WithEndpoint<EndpointThatThrowsOnInfrastructureStop>()
                .Done(c => c.EndpointsStarted)
                .Run();

            var logItem = context.Logs.FirstOrDefault(item => item.Message.Contains("stopping of feature startup task") && item.Level == LogLevel.Warn);
            Assert.IsNotNull(logItem);
            StringAssert.Contains("Exception occurred during stopping of feature startup task 'CustomTask'. System.InvalidOperationException: CustomTaskThrows", logItem.Message);
        }

        public class EndpointThatThrowsOnInfrastructureStop : EndpointConfigurationBuilder
        {
            public EndpointThatThrowsOnInfrastructureStop()
            {
                EndpointSetup<DefaultServer>(builder =>
                {
                    builder.UseTransport(new FakeTransport());
                });
            }

            class CustomFeature : Feature
            {
                public CustomFeature()
                {
                    EnableByDefault();
                }

                protected override void Setup(FeatureConfigurationContext context)
                {
                    context.RegisterStartupTask(new CustomTask());
                }

                class CustomTask : FeatureStartupTask
                {
                    protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
                    {
                        return Task.FromResult(0);
                    }

                    protected override async Task OnStop(IMessageSession session, CancellationToken cancellationToken = default)
                    {
                        await Task.Yield();

                        throw new InvalidOperationException("CustomTaskThrows");
                    }
                }
            }
        }
    }
}