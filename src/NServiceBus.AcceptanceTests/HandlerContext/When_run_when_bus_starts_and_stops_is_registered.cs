namespace NServiceBus.AcceptanceTests.HandlerContext
{
    using System;
    using global::NServiceBus.AcceptanceTesting;
    using global::NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_run_when_bus_starts_and_stops_is_registered : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_execute_start_and_stop()
        {
            var context = Scenario.Define<Context>()
                    .WithEndpoint<StartedEndpoint>()
                    .Done(c => c.StartCalled && c.StopCalled)
                    .Run(TimeSpan.FromSeconds(10));

            Assert.True(context.StartCalled);
            Assert.True(context.StopCalled);
        }

        public class Context : ScenarioContext
        {
            public bool StopCalled { get; set; }
            public bool StartCalled { get; set; }
        }

        public class StartedEndpoint : EndpointConfigurationBuilder
        {
            public StartedEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            class RunWhenBusStartsAndStops : IRunWhenBusStartsAndStops
            {
                public Context Context { get; set; }

                public void Start(IRunContext context)
                {
                    Context.StartCalled = true;
                }

                public void Stop(IRunContext context)
                {
                    Context.StopCalled = true;
                }
            }
        }
    }
}