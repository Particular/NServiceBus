namespace NServiceBus.AcceptanceTests.Core.Pipeline;

using System;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting;
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
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.OuterEventTtbr, Is.Not.Null, "Outer event should have TTBR settings applied");
            Assert.That(context.InnerEventTtbr, Is.Null, "Inner event should not have TTBR settings applied");
        }
    }

    public class Context : ScenarioContext
    {
        public bool InnerEventReceived { get; set; }
        public bool OuterEventReceived { get; set; }
        public DiscardIfNotReceivedBefore OuterEventTtbr { get; set; }
        public DiscardIfNotReceivedBefore InnerEventTtbr { get; set; }

        public void MaybeCompleted() => MarkAsCompleted(OuterEventReceived, InnerEventReceived);
    }

    class PublisherWithTtbr : EndpointConfigurationBuilder
    {
        public PublisherWithTtbr() =>
            EndpointSetup<DefaultServer>((c, r) =>
            {
                c.Pipeline.Register(new InnerPublishBehavior(), "publishes an additional event when publishing an OuterEvent");
                c.Pipeline.Register(new TtbrObserver((Context)r.ScenarioContext), "Checks outgoing messages for their TTBR setting");
            });

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

        class TtbrObserver(Context testContext) : Behavior<IDispatchContext>
        {
            public override Task Invoke(IDispatchContext context, Func<Task> next)
            {
                var outgoingMessage = context.Extensions.Get<OutgoingLogicalMessage>();
                if (outgoingMessage.MessageType == typeof(OuterEvent))
                {
                    testContext.OuterEventTtbr = context.Operations.Single().Properties.DiscardIfNotReceivedBefore;
                }

                if (outgoingMessage.MessageType == typeof(InnerEvent))
                {
                    testContext.InnerEventTtbr = context.Operations.Single().Properties.DiscardIfNotReceivedBefore;
                }

                return next();
            }
        }
    }

    public class Subscriber : EndpointConfigurationBuilder
    {
        public Subscriber() => EndpointSetup<DefaultServer>();

        [Handler]
        public class MultiHandler(Context testContext) : IHandleMessages<OuterEvent>, IHandleMessages<InnerEvent>
        {
            public Task Handle(OuterEvent message, IMessageHandlerContext context)
            {
                testContext.OuterEventReceived = true;
                testContext.MaybeCompleted();
                return Task.CompletedTask;
            }

            public Task Handle(InnerEvent message, IMessageHandlerContext context)
            {
                testContext.InnerEventReceived = true;
                testContext.MaybeCompleted();
                return Task.CompletedTask;
            }
        }
    }

    [TimeToBeReceived("00:30:00")]
    public class OuterEvent : IEvent;

    public class InnerEvent : IEvent;
}