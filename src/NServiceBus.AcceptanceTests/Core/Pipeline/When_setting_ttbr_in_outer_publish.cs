namespace NServiceBus.AcceptanceTests.Core.Pipeline
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using DeliveryConstraints;
    using EndpointTemplates;
    using NServiceBus.Pipeline;
    using NUnit.Framework;
    using Performance.TimeToBeReceived;

    public class When_setting_ttbr_in_outer_publish : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_apply_ttbr_to_inner_publish()
        {
            Requires.NativePubSubSupport();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<PublisherWithTtbr>(e => e
                    .When(s => s.Publish(new OuterEvent())))
                .WithEndpoint<Subscriber>()
                .Done(c => c.OuterEventReceived && c.InnerEventReceived)
                .Run();

            Assert.IsTrue(context.OuterEventHasTtbr, "Outer event should have TTBR settings applied");
            Assert.IsFalse(context.InnerEventHasTtbr, "Inner event should not have TTBR settings applied");
        }

        class Context : ScenarioContext
        {
            public bool InnerEventReceived { get; set; }
            public bool OuterEventReceived { get; set; }
            public bool OuterEventHasTtbr { get; set; }
            public bool InnerEventHasTtbr { get; set; }
        }

        class PublisherWithTtbr : EndpointConfigurationBuilder
        {
            public PublisherWithTtbr()
            {
                EndpointSetup<DefaultServer>((c, r) =>
                {
                    c.Pipeline.Register(new InnerPublishBehavior(), "publishes an additional event when publishing an OuterEvent");
                    c.Pipeline.Register(new TtbrObserver((Context)r.ScenarioContext), "Checks outgoing messages for their TTBR setting");
                });
            }

            // Behavior needs to be in the OutgoingPhysical stage as the TTBR settings are applied in the OutgoingLogical stage
            class InnerPublishBehavior : Behavior<IOutgoingPhysicalMessageContext>
            {
                public override async Task Invoke(IOutgoingPhysicalMessageContext context, Func<Task> next)
                {
                    await next();

                    if (context.Extensions.Get<OutgoingLogicalMessage>().MessageType == typeof(OuterEvent))
                    {
                        await context.Publish(new InnerEvent());
                    }
                }
            }

            class TtbrObserver : Behavior<IDispatchContext>
            {
                Context testContext;

                public TtbrObserver(Context testContext)
                {
                    this.testContext = testContext;
                }

                public override Task Invoke(IDispatchContext context, Func<Task> next)
                {
                    var outgoingMessage = context.Extensions.Get<OutgoingLogicalMessage>();
                    if (outgoingMessage.MessageType == typeof(OuterEvent))
                    {
                        testContext.OuterEventHasTtbr = context.Extensions.TryGetDeliveryConstraint<DiscardIfNotReceivedBefore>(out var _);
                    }

                    if (outgoingMessage.MessageType == typeof(InnerEvent))
                    {
                        testContext.InnerEventHasTtbr = context.Extensions.TryGetDeliveryConstraint<DiscardIfNotReceivedBefore>(out var _);
                    }

                    return next();
                }
            }
        }

        class Subscriber : EndpointConfigurationBuilder
        {
            public Subscriber()
            {
                EndpointSetup<DefaultServer>();
            }

            class EventHandler : IHandleMessages<OuterEvent>, IHandleMessages<InnerEvent>
            {
                Context testContext;

                public EventHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(OuterEvent message, IMessageHandlerContext context)
                {
                    testContext.OuterEventReceived = true;
                    return Task.FromResult(0);
                }

                public Task Handle(InnerEvent message, IMessageHandlerContext context)
                {
                    testContext.InnerEventReceived = true;
                    return Task.FromResult(0);
                }
            }
        }

        [TimeToBeReceived("00:30:00")]
        class OuterEvent : IEvent
        {
        }

        class InnerEvent : IEvent
        {
        }
    }
}