namespace NServiceBus.AcceptanceTests.DelayedDelivery
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NServiceBus.DelayedDelivery;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    public class When_deferring_outer_send : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_defer_inner_send()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<SenderWithNestedSend>(e => e
                    .When(s =>
                    {
                        var sendOptions = new SendOptions();
                        sendOptions.DelayDeliveryWith(TimeSpan.FromSeconds(2));
                        return s.Send(new DelayedMessage(), sendOptions);
                    }))
                .WithEndpoint<Receiver>()
                .Done(c => c.ReceivedNonDelayedMessage && c.ReceivedDelayedMessage)
                .Run();

            Assert.IsTrue(context.DelayedMessageDelayed, "should delay the message sent with 'DelayDeliveryWith'");
            Assert.IsFalse(context.NonDelayedMessageDelayed, "should not delay the message sent with default options");
        }

        class Context : ScenarioContext
        {
            public bool DelayedMessageDelayed { get; set; }
            public bool NonDelayedMessageDelayed { get; set; }

            public bool ReceivedNonDelayedMessage { get; set; }
            public bool ReceivedDelayedMessage { get; set; }
        }

        class SenderWithNestedSend : EndpointConfigurationBuilder
        {
            public SenderWithNestedSend() => EndpointSetup<DefaultServer>((c, r) =>
            {
                c.Pipeline.Register(new NestedSendBehavior(), "Sends an additional message when sending a delayed message");
                c.ConfigureTransport().Routing().RouteToEndpoint(typeof(DelayedMessage).Assembly, Conventions.EndpointNamingConvention(typeof(Receiver)));
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

            class DelayedMessageHandler : IHandleMessages<DelayedMessage>, IHandleMessages<NonDelayedMessage>
            {
                Context testContext;

                public DelayedMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(DelayedMessage message, IMessageHandlerContext context)
                {
                    testContext.ReceivedDelayedMessage = true;
                    testContext.DelayedMessageDelayed = context.MessageHeaders.TryGetValue(Headers.DeliverAt, out var _);
                    return Task.FromResult(0);
                }

                public Task Handle(NonDelayedMessage message, IMessageHandlerContext context)
                {
                    testContext.ReceivedNonDelayedMessage = true;
                    testContext.NonDelayedMessageDelayed = context.MessageHeaders.TryGetValue(Headers.DeliverAt, out var _);
                    return Task.FromResult(0);
                }
            }
        }

        class DelayedMessage : IMessage
        {
        }

        class NonDelayedMessage : IMessage
        {
        }
    }
}