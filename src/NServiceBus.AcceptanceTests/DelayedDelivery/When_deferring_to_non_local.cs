namespace NServiceBus.AcceptanceTests.DelayedDelivery;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using EndpointTemplates;
using NUnit.Framework;

public class When_deferring_to_non_local : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Message_should_be_received()
    {
        Requires.DelayedDelivery();

        var delay = TimeSpan.FromSeconds(5); // High value needed as most transports have multi second delay latency by default

        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(b => b.When((session, c) =>
            {
                var options = new SendOptions();

                options.DelayDeliveryWith(delay);

                c.SentAt = DateTimeOffset.UtcNow;

                return session.Send(new MyMessage(), options);
            }))
            .WithEndpoint<Receiver>()
            .Run();

        Assert.That(context.ReceivedAt - context.SentAt, Is.GreaterThanOrEqualTo(delay));
    }

    public class Context : ScenarioContext
    {
        public DateTimeOffset SentAt { get; set; }
        public DateTimeOffset ReceivedAt { get; set; }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() =>
            EndpointSetup<DefaultServer>(config =>
            {
                config.ConfigureRouting().RouteToEndpoint(typeof(MyMessage), typeof(Receiver));
            });
    }

    public class Receiver : EndpointConfigurationBuilder
    {
        public Receiver() => EndpointSetup<DefaultServer>();

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

    public class MyMessage : ICommand;
}
