namespace NServiceBus.AcceptanceTests.Sagas
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
                    .Done(c => c.ScheduleActionInvoked)
                    .Repeat(r => r.For(Transports.Default))
                  .Run();
        }

        public class Context : ScenarioContext
        {
            public bool ScheduleActionInvoked { get; set; }
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

                public void Start()
                {
                    Schedule.Every(TimeSpan.FromSeconds(5), "MyTask", () =>
                    {
                        Console.Out.WriteLine("Task invoked");
#pragma warning disable 0618
                        Configure.Instance.Builder.Build<Context>()
                            .ScheduleActionInvoked = true;
#pragma warning restore 0618
                    });
                }

                public void Stop()
                {

                }
            }
        }
    }


}