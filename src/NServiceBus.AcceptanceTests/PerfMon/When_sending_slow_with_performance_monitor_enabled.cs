namespace NServiceBus.AcceptanceTests.PerformanceMonitoring
{
    using System;
    using System.Threading;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_sending_slow_with_performance_monitor_enabled : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_receive_the_message()
        {
            var context = new Context();
            Scenario.Define(context)
                    .WithEndpoint<Sender>(b => b.Given((bus, c) => bus.Send(new MyMessage())))
                    .WithEndpoint<Receiver>()
                    .Done(c => c.WasCalled)
                    .Repeat(r => r.For(Transports.Default))
                    .Should(c => Assert.True(c.WasCalled, "The message handler should be called"))
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(configure => { }, builder => builder.EnablePerformanceCounters(new TimeSpan(0,0,0,0,1)))
                    .AddMapping<MyMessage>(typeof(Receiver));
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>(configure => { }, builder => builder.EnablePerformanceCounters(new TimeSpan(0, 0, 0, 0, 1)));
            }
        }

        public class MyMessage : IMessage
        {
        }

        public class MyMessageHandler : IHandleMessages<MyMessage>
        {
            public Context Context { get; set; }

            public IBus Bus { get; set; }

            public void Handle(MyMessage message)
            {
                Thread.Sleep(1000);
                Context.WasCalled = true;
            }
        }
    }
}