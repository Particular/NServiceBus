namespace NServiceBus.AcceptanceTests.Performance.TimeToBeReceived
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_TimeToBeReceived_has_expired_convention : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Message_should_not_be_received()
        {
            var context = await Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>(b => b.When((bus, c) => bus.SendLocalAsync(new MyMessage())))
                    .Run(TimeSpan.FromSeconds(10));

            Assert.IsFalse(context.WasCalled);
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
        }
        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(c=>c.Conventions().DefiningTimeToBeReceivedAs(messageType =>
                {
                    if (messageType == typeof(MyMessage))
                    {
                        return TimeSpan.Parse("00:00:00.0000001");
                    }
                    return TimeSpan.MaxValue;
                }));
            }
            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }

                public IBus Bus { get; set; }

                public Task Handle(MyMessage message)
                {
                    Context.WasCalled = true;
                    return Task.FromResult(0);
                }
            }
        }
        public class MyMessage : IMessage
        {
        }
    }
}
