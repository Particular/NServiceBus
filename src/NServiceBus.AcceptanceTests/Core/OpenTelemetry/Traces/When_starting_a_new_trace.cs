namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Traces;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using EndpointTemplates;
using NUnit.Framework;

public class When_starting_a_new_trace : OpenTelemetryAcceptanceTest
{
    [Test]
    public async Task Through_publish_options_should_create_new_trace_and_link_to_send()
    {
        var context = await Scenario.Define<PublishContext>()
            .WithEndpoint<Publisher>(b => b
                .When(ctx => ctx.SomeEventSubscribed, s =>
                {
                    var publishOptions = new PublishOptions();
                    publishOptions.StartNewTrace();
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
        Assert.AreEqual(1, publishMessageActivities.Count, "1 message is published as part of this test");
        Assert.AreEqual(1, receiveMessageActivities.Count, "1 message is received as part of this test");

        var publishRequest = publishMessageActivities[0];
        var receiveRequest = receiveMessageActivities[0];

        Assert.AreNotEqual(publishRequest.RootId, receiveRequest.RootId, "publish and receive operations are part of different root activities");
        Assert.IsNull(receiveRequest.ParentId, "incoming message does not have a parent, it's a root");

        ActivityLink link = receiveRequest.Links.FirstOrDefault();
        Assert.IsNotNull(link, "Receive has a link");
        Assert.AreEqual(publishRequest.TraceId, link.Context.TraceId, "receive is linked to publish operation");
    }

    [Test]
    public async Task Through_send_options_should_create_new_trace_and_link_to_send()
    {
        var context = await Scenario.Define<SendContext>()
            .WithEndpoint<Sender>(b => b
                .CustomConfig(c => c.ConfigureRouting().RouteToEndpoint(typeof(IncomingMessage), typeof(Receiver)))
                .When(s =>
                {
                    var sendOptions = new SendOptions();
                    sendOptions.StartNewTrace();
                    return s.Send(new IncomingMessage(), sendOptions);
                }))
            .WithEndpoint<Receiver>()
            .Done(c => c.OutgoingMessageReceived)
            .Run();

        var sendMessageActivities = NServicebusActivityListener.CompletedActivities.GetSendMessageActivities();
        var receiveMessageActivities = NServicebusActivityListener.CompletedActivities.GetReceiveMessageActivities();
        Assert.AreEqual(1, sendMessageActivities.Count, "1 message is sent as part of this test");
        Assert.AreEqual(1, receiveMessageActivities.Count, "1 message is received as part of this test");

        var sendRequest = sendMessageActivities[0];
        var receiveRequest = receiveMessageActivities[0];

        Assert.AreNotEqual(sendRequest.RootId, receiveRequest.RootId, "send and receive operations are part of different root activities");
        Assert.IsNull(receiveRequest.ParentId, "incoming message does not have a parent, it's a root");

        ActivityLink link = receiveRequest.Links.FirstOrDefault();
        Assert.IsNotNull(link, "Receive has a link");
        Assert.AreEqual(sendRequest.TraceId, link.Context.TraceId, "receive is linked to send operation");
    }

    public class PublishContext : ScenarioContext
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
                b.OnEndpointSubscribed<PublishContext>((s, context) =>
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
                    metadata.RegisterPublisherFor<ThisIsAnEvent>(typeof(Publisher));
                });

        public class ThisHandlesSomethingHandler : IHandleMessages<ThisIsAnEvent>
        {
            public ThisHandlesSomethingHandler(PublishContext testPublishContext)
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

            PublishContext testPublishContext;
        }
    }

    public class ThisIsAnEvent : IEvent
    {
    }

    class SendContext : ScenarioContext
    {
        public bool OutgoingMessageReceived { get; set; }
        public string SentMessageId { get; set; }
        public string MessageConversationId { get; set; }
        public IReadOnlyDictionary<string, string> SentMessageHeaders { get; set; }
    }

    class Sender : EndpointConfigurationBuilder
    {
        public Sender() => EndpointSetup<OpenTelemetryEnabledEndpoint>();
    }

    class Receiver : EndpointConfigurationBuilder
    {
        public Receiver() => EndpointSetup<OpenTelemetryEnabledEndpoint>();
        class MessageHandler : IHandleMessages<IncomingMessage>
        {
            SendContext testSendContext;

            public MessageHandler(SendContext testSendContext) => this.testSendContext = testSendContext;

            public Task Handle(IncomingMessage message, IMessageHandlerContext context)
            {
                testSendContext.SentMessageId = context.MessageId;
                testSendContext.MessageConversationId = context.MessageHeaders[Headers.ConversationId];
                testSendContext.OutgoingMessageReceived = true;
                testSendContext.SentMessageHeaders = new ReadOnlyDictionary<string, string>((IDictionary<string, string>)context.MessageHeaders);
                return Task.CompletedTask;
            }
        }
    }
    public class IncomingMessage : IMessage
    {
    }
}