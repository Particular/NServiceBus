namespace NServiceBus.AcceptanceTests.PerfMon.SLA
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_sending_with_SLA_enabled : NServiceBusAcceptanceTest
    {
        [Test]
        [Explicit("Since perf counters need to be enabled with powershell")]
        public async Task Should_have_perf_counter_set()
        {
            using (var counter = new PerformanceCounter("NServiceBus", "SLA violation countdown", "SendingWithSLAEnabled.Endpoint", false))
            {
                using (new Timer(state => CheckPerfCounter(counter), null, 0, 100))
                {
                    await Scenario.Define<Context>()
                        .WithEndpoint<Endpoint>(b => b.When((session, c) => session.SendLocal(new MyMessage())))
                        .Done(c => c.WasCalled)
                        .Repeat(r => r.For(Transports.Default))
                        .Should(c => Assert.True(c.WasCalled, "The message handler should be called"))
                        .Run();
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
                EndpointSetup<DefaultServer>(builder => builder.EnableSLAPerformanceCounter(TimeSpan.FromMinutes(10)));
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