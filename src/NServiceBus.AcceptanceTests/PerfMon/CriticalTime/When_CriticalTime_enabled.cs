// disable obsolete warnings. Tests will be removed in next major version
#pragma warning disable CS0618
namespace NServiceBus.AcceptanceTests.PerfMon.CriticalTime
{
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_CriticalTime_enabled : NServiceBusAcceptanceTest
    {
        [Test]
        [Explicit("Since perf counters need to be enabled with powershell")]
        public async Task Should_have_perf_counter_set()
        {
            using (var counter = new PerformanceCounter("NServiceBus", "Critical Time", "CriticaltimeEnabled.Endpoint", false))
            {
                using (new Timer(state => CheckPerfCounter(counter), null, 0, 100))
                {
                    var context = await Scenario.Define<Context>()
                        .WithEndpoint<Endpoint>(b => b.When((session, c) => session.SendLocal(new MyMessage())))
                        .Done(c => c.WasCalled)
                        .Run();

                    Assert.True(context.WasCalled, "The message handler should be called");
                }
            }
            Assert.Greater(counterValue, 0);
        }

        void CheckPerfCounter(PerformanceCounter counter)
        {
            float rawValue = counter.RawValue;
            if (rawValue > 0)
            {
                counterValue = rawValue;
            }
        }

        float counterValue;

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(builder => builder.EnableCriticalTimePerformanceCounter());
            }
        }

        public class MyMessage : IMessage
        {
        }

        public class MyMessageHandler : IHandleMessages<MyMessage>
        {
            public Context Context { get; set; }

            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                Context.WasCalled = true;
                return Task.FromResult(0);
            }
        }
    }
}
#pragma warning restore CS0618