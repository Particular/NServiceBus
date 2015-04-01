namespace NServiceBus.AcceptanceTests.HandlerContext
{
    using global::NServiceBus.AcceptanceTesting;
    using global::NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_run_when_bus_starts_and_stops_is_registered : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_execute_start_and_stop()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<StartedEndpoint>(e => e.When(c => c.NewStyleStartCalled && c.OldStyleStartCalled, b => b.Dispose()))
                    .Done(c => c.NewStyleStartCalled && c.NewStyleStopCalled && c.OldStyleStartCalled && c.OldStyleStopCalled)
                    .Run();

            Verify.AssertOldAndNewStyleStartAndStopsAreInvoked(context);
        }

        static class Verify
        {
            public static void AssertOldAndNewStyleStartAndStopsAreInvoked(Context context)
            {
                Assert.IsTrue(context.OldStyleStartCalled);
                Assert.IsTrue(context.OldStyleStopCalled);
                Assert.IsTrue(context.NewStyleStartCalled);
                Assert.IsTrue(context.NewStyleStopCalled);
            }
        }

        public class Context : ScenarioContext
        {
            public bool NewStyleStopCalled { get; set; }
            public bool NewStyleStartCalled { get; set; }
            public bool OldStyleStartCalled { get; set; }
            public bool OldStyleStopCalled { get; set; }
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
                    Context.NewStyleStartCalled = true;
                }

                public void Stop(IRunContext context)
                {
                    Context.NewStyleStopCalled = true;
                }
            }

            class WhenBusStartsAndStops : IWantToRunWhenBusStartsAndStops
            {
                public Context Context { get; set; }

                public void Start()
                {
                    Context.OldStyleStartCalled = true;
                }

                public void Stop()
                {
                    Context.OldStyleStopCalled = true;
                }
            }
        }
    }
}