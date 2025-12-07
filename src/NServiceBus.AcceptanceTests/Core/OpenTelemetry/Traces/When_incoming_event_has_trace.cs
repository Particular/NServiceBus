namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Traces;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Customization;
using NUnit.Framework;

public class When_incoming_event_has_trace : OpenTelemetryAcceptanceTest
{
    [Test, CancelAfter(10_000)]
    public async Task Should_correlate_trace_from_publish(CancellationToken cancellationToken = default)
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Publisher>(b => b
                .When(ctx => ctx.SomeEventSubscribed, s => s.Publish<SomeEvent>()))
            .WithEndpoint<Subscriber>(b => b.When((_, ctx) =>
            {
                if (ctx.HasNativePubSubSupport)
                {
                    ctx.SomeEventSubscribed = true;
                }

                return Task.CompletedTask;
            }))
            .Done(c => c.ReplyMessageReceived)
            .Run(cancellationToken);

        var incomingActivities = NServiceBusActivityListener.CompletedActivities.GetReceiveMessageActivities();
        var outgoingActivities = NServiceBusActivityListener.CompletedActivities.GetSendMessageActivities();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(incomingActivities, Has.Count.EqualTo(2), "2 messages are received as part of this test (event + reply)");
            Assert.That(outgoingActivities, Has.Count.EqualTo(1), "1 message is sent as part of this test (reply)");
            Assert.That(NServiceBusActivityListener.CompletedActivities.GetPublishEventActivities(), Has.Count.EqualTo(1), "1 event is published as part of this test");

            Assert.That(incomingActivities.Concat(outgoingActivities)
                .All(a => a.RootId == incomingActivities[0].RootId), Is.True, "all activities should belong to the same trace");
        }
    }
    public class Context : ScenarioContext
    {
        public bool SomeEventSubscribed { get; set; }
        public bool ReplyMessageReceived { get; set; }
        public string EventTraceParent { get; set; }
        public string ReplyTraceParent { get; set; }
    }

    class Publisher : EndpointConfigurationBuilder
    {
        public Publisher() =>
            EndpointSetup<OpenTelemetryEnabledEndpoint>(b =>
            {
                b.OnEndpointSubscribed<Context>((s, context) =>
                {
                    if (s.SubscriberEndpoint.Contains(Conventions.EndpointNamingConvention(typeof(Subscriber))))
                    {
                        if (s.MessageType == typeof(SomeEvent).AssemblyQualifiedName)
                        {
                            context.SomeEventSubscribed = true;
                        }
                    }
                });
            });

        class ReplyMessageHandler : IHandleMessages<ReplyMessage>
        {
            Context testContext;

            public ReplyMessageHandler(Context testContext) => this.testContext = testContext;

            public Task Handle(ReplyMessage message, IMessageHandlerContext context)
            {
                testContext.ReplyTraceParent = context.MessageHeaders[Headers.DiagnosticsTraceParent];
                testContext.ReplyMessageReceived = true;
                return Task.CompletedTask;
            }
        }
    }

    public class Subscriber : EndpointConfigurationBuilder
    {
        public Subscriber() =>
            EndpointSetup<OpenTelemetryEnabledEndpoint>(_ =>
                {
                },
                metadata =>
                {
                    metadata.RegisterPublisherFor<SomeEvent, Publisher>();
                });

        public class ThisHandlesSomethingHandler : IHandleMessages<SomeEvent>
        {
            public ThisHandlesSomethingHandler(Context testContext) => this.testContext = testContext;

            public Task Handle(SomeEvent @event, IMessageHandlerContext context)
            {
                testContext.EventTraceParent = context.MessageHeaders[Headers.DiagnosticsTraceParent];

                return context.Reply(new ReplyMessage());
            }

            readonly Context testContext;
        }
    }

    public class SomeEvent : IEvent
    {
    }

    public class ReplyMessage : IMessage
    {
    }
}