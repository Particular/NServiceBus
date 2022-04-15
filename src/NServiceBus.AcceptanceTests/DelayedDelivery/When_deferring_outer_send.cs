namespace NServiceBus.AcceptanceTests.DelayedDelivery
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using Features;
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
                if (!TestSuiteConstraints.Current.SupportsNativeDeferral)
                {
                    c.EnableFeature<TimeoutManager>();
                }
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
                    if (TestSuiteConstraints.Current.SupportsNativeDeferral)
                    {
                        testContext.DelayedMessageDelayed = context.MessageHeaders.TryGetValue(Headers.DeliverAt, out var _); // header value not set when routing to timeout manager    
                    }
                    else
                    {
                        testContext.DelayedMessageDelayed = context.MessageHeaders.TryGetValue("NServiceBus.Timeout.RouteExpiredTimeoutTo", out var _); // header value when routing to timeout manager queue
                    }

                    return Task.FromResult(0);
                }

                public Task Handle(NonDelayedMessage message, IMessageHandlerContext context)
                {
                    testContext.ReceivedNonDelayedMessage = true;
                    if (TestSuiteConstraints.Current.SupportsNativeDeferral)
                    {
                        testContext.NonDelayedMessageDelayed = context.MessageHeaders.TryGetValue(Headers.DeliverAt, out var _); // header value not set when routing to timeout manager    
                    }
                    else
                    {
                        testContext.NonDelayedMessageDelayed = context.MessageHeaders.TryGetValue("NServiceBus.Timeout.RouteExpiredTimeoutTo", out var _); // header value when routing to timeout manager queue
                    }

                    return Task.FromResult(0);
                }
            }
        }

        public class DelayedMessage : IMessage
        {
        }

        public class NonDelayedMessage : IMessage
        {
        }
    }
}