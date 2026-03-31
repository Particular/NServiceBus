namespace NServiceBus.AcceptanceTests.DelayedDelivery;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_deferring_a_message : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_delay_delivery()
    {
        Requires.DelayedDelivery();

        var delay = TimeSpan.FromSeconds(5); // High value needed as most transports have multi second delay latency by default

        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(b => b.When((session, c) =>
            {
                var options = new SendOptions();

                options.DelayDeliveryWith(delay);
                options.RouteToThisEndpoint();

                c.SentAt = DateTimeOffset.UtcNow;

                return session.Send(new MyMessage(), options);
            }))
            .Run();

        var sendReceiveDifference = context.ReceivedAt - context.SentAt;
        Assert.That(sendReceiveDifference, Is.GreaterThanOrEqualTo(delay));
    }

    public class Context : ScenarioContext
    {
        public DateTimeOffset SentAt { get; set; }
        public DateTimeOffset ReceivedAt { get; set; }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() => EndpointSetup<DefaultServer>();

        [Handler]
        public class MyMessageHandler(Context testContext) : IHandleMessages<MyMessage>
        {
            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                testContext.ReceivedAt = DateTimeOffset.UtcNow;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class MyMessage : IMessage;
}
