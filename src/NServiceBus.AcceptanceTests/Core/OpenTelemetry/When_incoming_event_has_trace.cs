namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry;

using System;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Customization;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NUnit.Framework;

[NonParallelizable] // Ensure only activities for the current test are captured
public class When_incoming_event_has_trace : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_correlate_trace_from_publish()
    {
        using var activityListener = TestingActivityListener.SetupNServiceBusDiagnosticListener();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<Publisher>(b => b
                .When(ctx => ctx.SomeEventSubscribed, s => s.Publish<ThisIsAnEvent>()))
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

        var incomingActivities = activityListener.CompletedActivities.GetIncomingActivities();
        var outgoingActivities = activityListener.CompletedActivities.GetOutgoingActivities();

        Assert.AreEqual(2, incomingActivities.Count, "2 messages are received as part of this test (event + reply)");
        Assert.AreEqual(1, outgoingActivities.Count, "1 message is sent as part of this test (reply)");
        Assert.AreEqual(1, activityListener.CompletedActivities.GetOutgoingEventActivities().Count, "1 event is published as part of this test");

        Assert.IsTrue(incomingActivities.Concat(outgoingActivities)
            .All(a => a.RootId == incomingActivities[0].RootId), "all activities should belong to the same trace");

        //TODO this will currently fail on CI because we don't set the is_control_message tag (or header) yet.
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
                        if (s.MessageType == typeof(ThisIsAnEvent).AssemblyQualifiedName)
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
            EndpointSetup<DefaultServer>(c =>
                {
                },
                metadata =>
                {
                    metadata.RegisterPublisherFor<ThisIsAnEvent>(typeof(Publisher));
                });

        public class ThisHandlesSomethingHandler : IHandleMessages<ThisIsAnEvent>
        {
            public ThisHandlesSomethingHandler(Context testContext) => this.testContext = testContext;

            public Task Handle(ThisIsAnEvent @event, IMessageHandlerContext context)
            {
                testContext.EventTraceParent = context.MessageHeaders[Headers.DiagnosticsTraceParent];

                return context.Reply(new ReplyMessage());
            }

            readonly Context testContext;
        }
    }

    public class ThisIsAnEvent : IEvent
    {
    }

    public class ReplyMessage : IMessage
    {
    }
}