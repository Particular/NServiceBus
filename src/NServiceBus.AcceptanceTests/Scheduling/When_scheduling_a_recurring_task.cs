namespace NServiceBus.AcceptanceTests.Scheduling
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;

    public class When_scheduling_a_recurring_task : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_execute_the_task()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<SchedulingEndpoint>()
                .Done(c => c.InvokedAt.HasValue)
                .Run(TimeSpan.FromSeconds(60));

            Assert.True(context.InvokedAt.HasValue);
            Assert.Greater(context.InvokedAt.Value - context.RequestedAt, TimeSpan.FromMilliseconds(5));
        }

        class Context : ScenarioContext
        {
            public DateTime? InvokedAt { get; set; }
            public DateTime RequestedAt { get; set; }
        }

        class SetupScheduledAction : Feature
        {
            protected override void Setup(FeatureConfigurationContext context)
            {
                context.RegisterStartupTask(b => new SetupScheduledActionTask(b.GetService<Context>()));
            }
        }

        class SetupScheduledActionTask : FeatureStartupTask
        {
            public SetupScheduledActionTask(Context context)
            {
                this.context = context;
            }

            protected override Task OnStart(IMessageSession session)
            {
                context.RequestedAt = DateTime.UtcNow;

                return session.ScheduleEvery(TimeSpan.FromMilliseconds(5), "MyTask", c =>
                {
                    context.InvokedAt = DateTime.UtcNow;
                    return Task.FromResult(0);
                });
            }

            protected override Task OnStop(IMessageSession session)
            {
                return Task.FromResult(0);
            }

            Context context;
        }

        class SchedulingEndpoint : EndpointConfigurationBuilder
        {
            public SchedulingEndpoint()
            {
                EndpointSetup<DefaultServer>(config =>
                {
                    config.EnableFeature<TimeoutManager>();
                    config.EnableFeature<SetupScheduledAction>();
                });
            }
        }
    }
}