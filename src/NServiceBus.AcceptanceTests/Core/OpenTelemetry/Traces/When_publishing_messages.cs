﻿namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Traces;

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using NUnit.Framework;

public class When_publishing_messages : OpenTelemetryAcceptanceTest
{
    [Test]
    public async Task Should_create_outgoing_event_span()
    {
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

        var outgoingEventActivities = NServicebusActivityListener.CompletedActivities.GetPublishEventActivities();
        Assert.That(outgoingEventActivities, Has.Count.EqualTo(1), "1 event is being published");

        var publishedMessage = outgoingEventActivities.Single();
        publishedMessage.VerifyUniqueTags();
        Assert.Multiple(() =>
        {
            Assert.That(publishedMessage.DisplayName, Is.EqualTo("publish event"));
            Assert.That(publishedMessage.ParentId, Is.Null, "publishes without ambient span should start a new trace");
        });

        var sentMessageTags = publishedMessage.Tags.ToImmutableDictionary();
        sentMessageTags.VerifyTag("nservicebus.message_id", context.PublishedMessageId);

        Assert.That(context.TraceParentHeader, Is.Not.Null, "tracing header should be set on the published event");
    }

    [Test]
    public async Task Should_create_child_on_receive_when_requested_via_options()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Publisher>(b => b
                .When(ctx => ctx.SomeEventSubscribed, s =>
                {
                    var publishOptions = new PublishOptions();
                    publishOptions.ContinueExistingTraceOnReceive();
                    return s.Publish(new ThisIsAnEvent(), publishOptions);
                }))
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

        var publishMessageActivities = NServicebusActivityListener.CompletedActivities.GetPublishEventActivities();
        var receiveMessageActivities = NServicebusActivityListener.CompletedActivities.GetReceiveMessageActivities();
        Assert.Multiple(() =>
        {
            Assert.That(publishMessageActivities, Has.Count.EqualTo(1), "1 message is published as part of this test");
            Assert.That(receiveMessageActivities, Has.Count.EqualTo(1), "1 message is received as part of this test");
        });

        var publishRequest = publishMessageActivities[0];
        var receiveRequest = receiveMessageActivities[0];

        Assert.Multiple(() =>
        {
            Assert.That(receiveRequest.RootId, Is.EqualTo(publishRequest.RootId), "publish and receive operations are part the same root activity");
            Assert.That(receiveRequest.ParentId, Is.Not.Null, "incoming message does have a parent");
        });

        Assert.That(receiveRequest.Links, Is.Empty, "receive does not have links");
    }

    [Test]
    public async Task Should_create_new_linked_trace_on_receive_by_default()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Publisher>(b => b
                .When(ctx => ctx.SomeEventSubscribed, s =>
                {
                    return s.Publish(new ThisIsAnEvent());
                }))
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

        var publishMessageActivities = NServicebusActivityListener.CompletedActivities.GetPublishEventActivities();
        var receiveMessageActivities = NServicebusActivityListener.CompletedActivities.GetReceiveMessageActivities();
        Assert.Multiple(() =>
        {
            Assert.That(publishMessageActivities, Has.Count.EqualTo(1), "1 message is published as part of this test");
            Assert.That(receiveMessageActivities, Has.Count.EqualTo(1), "1 message is received as part of this test");
        });

        var publishRequest = publishMessageActivities[0];
        var receiveRequest = receiveMessageActivities[0];

        Assert.Multiple(() =>
        {
            Assert.That(receiveRequest.RootId, Is.Not.EqualTo(publishRequest.RootId), "publish and receive operations are part of different root activities");
            Assert.That(receiveRequest.ParentId, Is.Null, "incoming message does not have a parent, it's a root");
        });

        ActivityLink link = receiveRequest.Links.FirstOrDefault();
        Assert.That(link, Is.Not.EqualTo(default(ActivityLink)), "Receive has a link");
        Assert.That(link.Context.TraceId, Is.EqualTo(publishRequest.TraceId), "receive is linked to publish operation");
    }

    public class Context : ScenarioContext
    {
        public bool OutgoingEventReceived { get; set; }
        public string PublishedMessageId { get; set; }
        public string TraceParentHeader { get; set; }
        public bool SomeEventSubscribed { get; set; }
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
        public Subscriber() =>
            EndpointSetup<OpenTelemetryEnabledEndpoint>(c =>
            {
            },
                metadata =>
                {
                    metadata.RegisterPublisherFor<ThisIsAnEvent, Publisher>();
                });

        public class ThisHandlesSomethingHandler : IHandleMessages<ThisIsAnEvent>
        {
            public ThisHandlesSomethingHandler(Context testPublishContext)
            {
                this.testPublishContext = testPublishContext;
            }

            public Task Handle(ThisIsAnEvent @event, IMessageHandlerContext context)
            {
                if (context.MessageHeaders.TryGetValue(Headers.DiagnosticsTraceParent, out var traceParentHeader))
                {
                    testPublishContext.TraceParentHeader = traceParentHeader;
                }

                testPublishContext.PublishedMessageId = context.MessageId;
                testPublishContext.OutgoingEventReceived = true;
                return Task.CompletedTask;
            }

            Context testPublishContext;
        }
    }

    public class ThisIsAnEvent : IEvent
    {
    }
}