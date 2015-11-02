﻿namespace NServiceBus.AcceptanceTests.Scheduling
{
    using System;
    using System.Threading.Tasks;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Features;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_scheduling_a_recurring_task : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_execute_the_task()
        {
            await Scenario.Define<Context>()
                    .WithEndpoint<SchedulingEndpoint>()
                    .Done(c => c.InvokedAt.HasValue)
                    .Repeat(r => r.For(Transports.Default))
                    .Should(c =>
                    {
                        Assert.True(c.InvokedAt.HasValue);
                        Assert.Greater(c.InvokedAt.Value - c.RequestedAt, TimeSpan.FromSeconds(5));
                    })
                  .Run(TimeSpan.FromSeconds(20));
        }

        public class Context : ScenarioContext
        {
            public DateTime? InvokedAt{ get; set; }
            public DateTime RequestedAt{ get; set; }
        }

        public class SchedulingEndpoint : EndpointConfigurationBuilder
        {
            public SchedulingEndpoint()
            {
                EndpointSetup<DefaultServer>(config => config.EnableFeature<TimeoutManager>());
            }

            class SetupScheduledAction : IWantToRunWhenBusStartsAndStops
            {
                public Context Context { get; set; }
                public Task StartAsync(IBusContext context)
                {
                    Context.RequestedAt = DateTime.UtcNow;

                    context.ScheduleEvery(TimeSpan.FromSeconds(5), "MyTask", () =>
                    {
                        Context.InvokedAt = DateTime.UtcNow;
                    });
                    return Task.FromResult(0);
                }

                public Task StopAsync(IBusContext context)
                {
                    return Task.FromResult(0);
                }
            }
        }
    }


}