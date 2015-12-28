namespace NServiceBus.AcceptanceTests.PerfMon.CriticalTime
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NUnit.Framework;

    public class When_deferring_a_message : NServiceBusAcceptanceTest
    {
        float counterValue;

        [Test]
        [Explicit("Since perf counters need to be enabled with powershell")]
        public async Task Critical_time_should_not_include_the_time_message_was_waiting_in_the_timeout_store()
        {
            using (var counter = new PerformanceCounter("NServiceBus", "Critical Time", "DeferringAMessage.Endpoint", false))
            using (new Timer(state => CheckPerfCounter(counter), null, 0, 100))
            {
                await Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>(b => b.When((bus, c) =>
                    {
                        var options = new SendOptions();

                        options.DelayDeliveryWith(TimeSpan.FromMilliseconds(1));
                        options.RouteToLocalEndpointInstance();

                        return bus.Send(new MyMessage(), options);
                    }))
                    .Done(c => c.WasCalled)
                    .Repeat(r => r.For(Transports.Default))
                    .Should(c => Assert.True(c.WasCalled, "The message handler should be called"))
                    .Run();
            }
            Assert.Greater(counterValue, 0, "Critical time has not been recorded");
            Assert.Less(counterValue, 2);
        }

        void CheckPerfCounter(PerformanceCounter counter)
        {
            float rawValue = counter.RawValue;
            if (rawValue > 0)
            {
                counterValue = rawValue;
            }
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(builder => builder.EnableCriticalTimePerformanceCounter())
                    .AddMapping<MyMessage>(typeof(Endpoint));
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