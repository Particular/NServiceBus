﻿namespace NServiceBus.AcceptanceTests.PerfMon.CriticalTime
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    public class When_deferring_a_message : NServiceBusAcceptanceTest
    {
        [Test]
        [Explicit("Since perf counters need to be enabled with powershell")]
        public async Task Critical_time_should_not_include_the_time_message_was_waiting_in_the_timeout_store()
        {
            using (var counter = new PerformanceCounter("NServiceBus", "Critical Time", "DeferringAMessage.Endpoint", false))
            {
                using (new Timer(state => CheckPerfCounter(counter), null, 0, 100))
                {
                    var context = await Scenario.Define<Context>()
                        .WithEndpoint<Endpoint>(b => b.When((session, c) =>
                        {
                            var options = new SendOptions();

                            options.DelayDeliveryWith(TimeSpan.FromMilliseconds(1));
                            options.RouteToThisEndpoint();

                            return session.Send(new MyMessage(), options);
                        }))
                        .Done(c => c.WasCalled)
                        .Run();

                    Assert.True(context.WasCalled, "The message handler should be called");
                }
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

        float counterValue;

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(builder =>
                {
                    builder.EnableCriticalTimePerformanceCounter();
                    builder.EnableFeature<TimeoutManager>();
                })
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