namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry;

using System;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Customization;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NUnit.Framework;

public class When_incoming_event_has_trace : OpenTelemetryAcceptanceTest
{
    [Test]
    public async Task Should_correlate_trace_from_publish()
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
            .Run(TimeSpan.FromSeconds(10));

        var incomingActivities = NServicebusActivityListener.CompletedActivities.GetReceiveMessageActivities();
        var outgoingActivities = NServicebusActivityListener.CompletedActivities.GetSendMessageActivities();

        Assert.AreEqual(2, incomingActivities.Count, "2 messages are received as part of this test (event + reply)");
        Assert.AreEqual(1, outgoingActivities.Count, "1 message is sent as part of this test (reply)");
        Assert.AreEqual(1, NServicebusActivityListener.CompletedActivities.GetPublishEventActivities().Count, "1 event is published as part of this test");

        Assert.IsTrue(incomingActivities.Concat(outgoingActivities)
            .All(a => a.RootId == incomingActivities[0].RootId), "all activities should belong to the same trace");
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
            EndpointSetup<DefaultPublisher>(b =>
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
            EndpointSetup<DefaultServer>(_ =>
                {
                },
                metadata =>
                {
                    metadata.RegisterPublisherFor<SomeEvent>(typeof(Publisher));
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