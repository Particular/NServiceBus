namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry
{
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTesting.Customization;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    [NonParallelizable] // Ensure only activities for the current test are captured
    public class When_publishing_messages : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_create_outgoing_event_span()
        {
            using var activityListener = TestingActivityListener.SetupNServiceBusDiagnosticListener();
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Publisher>(b => b
                    .When(ctx => ctx.SomeEventSubscribed, s => s.Publish<ThisIsAnEvent>()))
                .WithEndpoint<Subscriber>(b => b.When((session, ctx) =>
                {
                    if (ctx.HasNativePubSubSupport)
                    {
                        ctx.SomeEventSubscribed = true;
                    }

                    return Task.CompletedTask;
                }))
                .Done(c => c.OutgoingEventReceived)
                .Run();

            Assert.AreEqual(activityListener.StartedActivities.Count, activityListener.CompletedActivities.Count, "all activities should be completed");

            var outgoingEventActivities = activityListener.CompletedActivities.GetOutgoingEventActivities();
            Assert.AreEqual(1, outgoingEventActivities.Count, "1 event is being published");

            var publishedMessage = outgoingEventActivities.Single();
            publishedMessage.VerifyUniqueTags();
            Assert.AreEqual("publish event", publishedMessage.DisplayName);
            Assert.IsNull(publishedMessage.ParentId, "publishes without ambient span should start a new trace");

            var sentMessageTags = publishedMessage.Tags.ToImmutableDictionary();
            sentMessageTags.VerifyTag("nservicebus.message_id", context.SentMessageId);

            Assert.IsNotNull(context.TraceParentHeader, "tracing header should be set on the published event");
        }

        public class Context : ScenarioContext
        {
            public bool OutgoingEventReceived { get; set; }
            public string SentMessageId { get; set; }
            public string TraceParentHeader { get; set; }
            public bool SomeEventSubscribed { get; set; }
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
        }

        public class Subscriber : EndpointConfigurationBuilder
        {
            public Subscriber()
            {
                EndpointSetup<DefaultServer>(c =>
                    {
                    },
                    metadata =>
                    {
                        metadata.RegisterPublisherFor<ThisIsAnEvent>(typeof(Publisher));
                    });
            }

            public class ThisHandlesSomethingHandler : IHandleMessages<ThisIsAnEvent>
            {
                public ThisHandlesSomethingHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(ThisIsAnEvent @event, IMessageHandlerContext context)
                {
                    if (context.MessageHeaders.TryGetValue(Headers.DiagnosticsTraceParent, out var traceParentHeader))
                    {
                        testContext.TraceParentHeader = traceParentHeader;
                    }

                    testContext.SentMessageId = context.MessageId;
                    testContext.OutgoingEventReceived = true;
                    return Task.CompletedTask;
                }

                Context testContext;
            }
        }

        public class ThisIsAnEvent : IEvent
        {
        }
    }
}