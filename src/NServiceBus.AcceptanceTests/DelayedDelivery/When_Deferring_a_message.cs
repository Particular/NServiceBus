namespace NServiceBus.AcceptanceTests.DelayedDelivery
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    public class When_deferring_a_message : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_delay_delivery()
        {
            var delay = TimeSpan.FromSeconds(2);

            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.When((session, c) =>
                {
                    var options = new SendOptions();

                    options.DelayDeliveryWith(delay);
                    options.RouteToThisEndpoint();

                    c.SentAt = DateTimeOffset.UtcNow;

                    return session.Send(new MyMessage(), options);
                }))
                .Done(c => c.WasCalled)
                .Run();

            Assert.GreaterOrEqual(context.ReceivedAt - context.SentAt, delay);
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
            public DateTimeOffset SentAt { get; set; }
            public DateTimeOffset ReceivedAt { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(config => config.EnableFeature<TimeoutManager>());
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public MyMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    testContext.ReceivedAt = DateTimeOffset.UtcNow;
                    testContext.WasCalled = true;
                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        public class MyMessage : IMessage
        {
        }
    }
}