namespace NServiceBus.AcceptanceTests.DelayedDelivery;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using EndpointTemplates;
using NServiceBus.Pipeline;
using NUnit.Framework;

public class When_deferring_outer_send : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_not_defer_inner_send()
    {
        Requires.DelayedDelivery();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<SenderWithNestedSend>(e => e
                .When(s =>
                {
                    var sendOptions = new SendOptions();
                    sendOptions.DelayDeliveryWith(TimeSpan.FromSeconds(2));
                    return s.Send(new DelayedMessage(), sendOptions);
                }))
            .WithEndpoint<Receiver>()
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.DelayedMessageDelayed, Is.True, "should delay the message sent with 'DelayDeliveryWith'");
            Assert.That(context.NonDelayedMessageDelayed, Is.False, "should not delay the message sent with default options");
        }
    }

    class Context : ScenarioContext
    {
        public bool DelayedMessageDelayed { get; set; }
        public bool NonDelayedMessageDelayed { get; set; }

        public bool ReceivedNonDelayedMessage { get; set; }
        public bool ReceivedDelayedMessage { get; set; }

        public void MaybeCompleted() => MarkAsCompleted(ReceivedDelayedMessage, ReceivedNonDelayedMessage);
    }

    class SenderWithNestedSend : EndpointConfigurationBuilder
    {
        public SenderWithNestedSend() => EndpointSetup<DefaultServer>((c, r) =>
        {
            c.Pipeline.Register(new NestedSendBehavior(), "Sends an additional message when sending a delayed message");
            c.ConfigureRouting().RouteToEndpoint(typeof(DelayedMessage).Assembly, Conventions.EndpointNamingConvention(typeof(Receiver)));
        });

        class NestedSendBehavior : Behavior<IOutgoingSendContext>
        {
            public override async Task Invoke(IOutgoingSendContext context, Func<Task> next)
            {
                await next();
                if (context.Message.MessageType == typeof(DelayedMessage))
                {
                    await context.Send(new NonDelayedMessage()); // use default options
                }
            }
        }
    }

    class Receiver : EndpointConfigurationBuilder
    {
        public Receiver() => EndpointSetup<DefaultServer>();

        class DelayedMessageHandler(Context testContext) : IHandleMessages<DelayedMessage>, IHandleMessages<NonDelayedMessage>
        {
            public Task Handle(DelayedMessage message, IMessageHandlerContext context)
            {
                testContext.ReceivedDelayedMessage = true;
                testContext.DelayedMessageDelayed = context.MessageHeaders.TryGetValue(Headers.DeliverAt, out var _); // header value not set when routing to timeout manager
                testContext.MaybeCompleted();
                return Task.CompletedTask;
            }

            public Task Handle(NonDelayedMessage message, IMessageHandlerContext context)
            {
                testContext.ReceivedNonDelayedMessage = true;
                testContext.NonDelayedMessageDelayed = context.MessageHeaders.TryGetValue(Headers.DeliverAt, out var _); // header value not set when routing to timeout manager
                testContext.MaybeCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class DelayedMessage : IMessage;

    public class NonDelayedMessage : IMessage;
}