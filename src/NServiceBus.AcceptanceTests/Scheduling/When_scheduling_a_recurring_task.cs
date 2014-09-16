namespace NServiceBus.AcceptanceTests.Scheduling
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_scheduling_a_recurring_task : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_execute_the_task()
        {
            Scenario.Define<Context>()
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
                EndpointSetup<DefaultServer>();
            }

            class SetupScheduledAction : IWantToRunWhenBusStartsAndStops
            {
                public Schedule Schedule { get; set; }
                public Context Context { get; set; }
                public void Start()
                {
                    Context.RequestedAt = DateTime.UtcNow;

                    Schedule.Every(TimeSpan.FromSeconds(5), "MyTask", () =>
                    {
                        Context.InvokedAt = DateTime.UtcNow;
                    });
                }

                public void Stop()
                {
                }
            }
        }
    }


}