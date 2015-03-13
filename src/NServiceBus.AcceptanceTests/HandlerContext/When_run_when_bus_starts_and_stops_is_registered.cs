namespace NServiceBus.AcceptanceTests.HandlerContext
{
    using System.Collections.Generic;
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
            Verify.AssertNewStyleStartAndStopsAreInvokedFirst(context);
            Verify.AssertOldStyleStartAndStopsAreInvokedSecond(context);
        }

        static class Verify
        {
            public static void AssertNewStyleStartAndStopsAreInvokedFirst(Context context)
            {
                Assert.AreEqual("NewStyle.Start", context.StartsAndStopsExecuted[0]);
                Assert.AreEqual("NewStyle.Stop", context.StartsAndStopsExecuted[2]);
            }

            public static void AssertOldStyleStartAndStopsAreInvokedSecond(Context context)
            {
                Assert.AreEqual("OldStyle.Start", context.StartsAndStopsExecuted[1]);
                Assert.AreEqual("OldStyle.Stop", context.StartsAndStopsExecuted[3]);
            }

            public static void AssertOldAndNewStyleStartAndStopsAreInvoked(Context context)
            {
                Assert.AreEqual(4, context.StartsAndStopsExecuted.Count, "Old and new style starts and stops should be invoked");
            }
        }

        public class Context : ScenarioContext
        {
            public Context()
            {
                StartsAndStopsExecuted = new List<string>();
            }

            public bool NewStyleStopCalled { get; set; }
            public bool NewStyleStartCalled { get; set; }
            public bool OldStyleStartCalled { get; set; }
            public bool OldStyleStopCalled { get; set; }

            public List<string> StartsAndStopsExecuted { get; private set; }
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
                    Context.StartsAndStopsExecuted.Add("NewStyle.Start");
                    Context.NewStyleStartCalled = true;
                }

                public void Stop(IRunContext context)
                {
                    Context.StartsAndStopsExecuted.Add("NewStyle.Stop");
                    Context.NewStyleStopCalled = true;
                }
            }

            class WhenBusStartsAndStops : IWantToRunWhenBusStartsAndStops
            {
                public Context Context { get; set; }

                public void Start()
                {
                    Context.StartsAndStopsExecuted.Add("OldStyle.Start");
                    Context.OldStyleStartCalled = true;
                }

                public void Stop()
                {
                    Context.StartsAndStopsExecuted.Add("OldStyle.Stop");
                    Context.OldStyleStopCalled = true;
                }
            }
        }
    }
}