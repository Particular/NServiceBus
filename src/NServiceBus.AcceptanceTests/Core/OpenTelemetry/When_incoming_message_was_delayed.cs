namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry;

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Customization;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NUnit.Framework;

public class When_incoming_message_was_delayed : OpenTelemetryAcceptanceTest // assuming W3C trace!
{
    [Test]
    public async Task By_sendoptions_Should_create_new_trace_and_link_to_send()
    {
        Requires.DelayedDelivery();
        var context = await Scenario.Define<Context>()
            .WithEndpoint<TestEndpoint>(b => b
                .CustomConfig(c => c.ConfigureRouting().RouteToEndpoint(typeof(IncomingMessage), typeof(ReplyingEndpoint)))
                .When(s =>
                {
                    var sendOptions = new SendOptions();
                    sendOptions.DelayDeliveryWith(TimeSpan.FromMilliseconds(5));
                    return s.Send(new IncomingMessage(), sendOptions);
                }))
            .WithEndpoint<ReplyingEndpoint>()
            .Done(c => c.ReplyMessageReceived)
            .Run();

        var incomingMessageActivities = NServicebusActivityListener.CompletedActivities.GetReceiveMessageActivities();
        var outgoingMessageActivities = NServicebusActivityListener.CompletedActivities.GetSendMessageActivities();
        Assert.AreEqual(2, incomingMessageActivities.Count, "2 messages are received as part of this test");
        Assert.AreEqual(2, outgoingMessageActivities.Count, "2 messages are sent as part of this test");

        var sendRequest = outgoingMessageActivities[0];
        var receiveRequest = incomingMessageActivities[0];
        var sendReply = outgoingMessageActivities[1];
        var receiveReply = incomingMessageActivities[1];

        Assert.AreNotEqual(sendRequest.RootId, receiveRequest.RootId, "send and receive operations are part of different root activities");
        Assert.AreEqual(sendRequest.Id, receiveRequest.ParentId, "first incoming message is correlated to the first send operation");
        Assert.AreEqual(sendRequest.RootId, sendReply.RootId, "first send operation is the root activity of the reply");
        Assert.AreEqual(sendReply.Id, receiveReply.ParentId, "second incoming message is correlated to the second send operation");
        Assert.AreEqual(sendRequest.RootId, receiveReply.RootId, "first send operation is the root activity");

        Assert.AreEqual(context.IncomingMessageId, sendRequest.Tags.ToImmutableDictionary()["nservicebus.message_id"]);
        Assert.AreEqual(context.IncomingMessageId, receiveRequest.Tags.ToImmutableDictionary()["nservicebus.message_id"]);
        Assert.AreEqual(context.ReplyMessageId, sendReply.Tags.ToImmutableDictionary()["nservicebus.message_id"]);
        Assert.AreEqual(context.ReplyMessageId, receiveReply.Tags.ToImmutableDictionary()["nservicebus.message_id"]);
    }

    [Test]
    public async Task By_retry_Should_create_new_trace_and_link_to_send()
    {
        Requires.DelayedDelivery();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<RetryEndpoint>(b => b
                .CustomConfig(c => c.ConfigureRouting().RouteToEndpoint(typeof(IncomingMessage), typeof(ReplyingEndpoint)))
                .When(session => session.SendLocal(new MessageToBeRetried()))
                .DoNotFailOnErrorMessages())
            .Done(c => !c.FailedMessages.IsEmpty)
            .Run(TimeSpan.FromSeconds(120));

        var incomingMessageActivities = NServicebusActivityListener.CompletedActivities.GetReceiveMessageActivities();
        var outgoingMessageActivities = NServicebusActivityListener.CompletedActivities.GetSendMessageActivities();
        Assert.AreEqual(2, incomingMessageActivities.Count, "2 messages are received as part of this test");
        Assert.AreEqual(2, outgoingMessageActivities.Count, "2 messages are sent as part of this test");

        var sendRequest = outgoingMessageActivities[0];
        var receiveRequest = incomingMessageActivities[0];
        var sendReply = outgoingMessageActivities[1];
        var receiveReply = incomingMessageActivities[1];

        Assert.AreNotEqual(sendRequest.RootId, receiveRequest.RootId, "send and receive operations are part of different root activities");
        Assert.AreEqual(sendRequest.Id, receiveRequest.ParentId, "first incoming message is correlated to the first send operation");
        Assert.AreEqual(sendRequest.RootId, sendReply.RootId, "first send operation is the root activity of the reply");
        Assert.AreEqual(sendReply.Id, receiveReply.ParentId, "second incoming message is correlated to the second send operation");
        Assert.AreEqual(sendRequest.RootId, receiveReply.RootId, "first send operation is the root activity");

        Assert.AreEqual(context.IncomingMessageId, sendRequest.Tags.ToImmutableDictionary()["nservicebus.message_id"]);
        Assert.AreEqual(context.IncomingMessageId, receiveRequest.Tags.ToImmutableDictionary()["nservicebus.message_id"]);
        Assert.AreEqual(context.ReplyMessageId, sendReply.Tags.ToImmutableDictionary()["nservicebus.message_id"]);
        Assert.AreEqual(context.ReplyMessageId, receiveReply.Tags.ToImmutableDictionary()["nservicebus.message_id"]);
    }

    class Context : ScenarioContext
    {
        public bool ReplyMessageReceived { get; set; }
        public string IncomingMessageId { get; set; }
        public string ReplyMessageId { get; set; }
        public bool IncomingMessageReceived { get; set; }
    }

    class ReceivingEndpoint : EndpointConfigurationBuilder
    {
        public ReceivingEndpoint() => EndpointSetup<OpenTelemetryEnabledEndpoint>();

        class MessageHandler : IHandleMessages<IncomingMessage>
        {
            readonly Context testContext;

            public MessageHandler(Context testContext) => this.testContext = testContext;

            public Task Handle(IncomingMessage message, IMessageHandlerContext context)
            {
                testContext.IncomingMessageId = context.MessageId;
                testContext.IncomingMessageReceived = true;
                return Task.CompletedTask;
            }
        }
    }

    class ReplyingEndpoint : EndpointConfigurationBuilder
    {
        public ReplyingEndpoint() => EndpointSetup<OpenTelemetryEnabledEndpoint>();

        class MessageHandler : IHandleMessages<IncomingMessage>
        {
            readonly Context testContext;

            public MessageHandler(Context testContext) => this.testContext = testContext;

            public Task Handle(IncomingMessage message, IMessageHandlerContext context)
            {
                testContext.IncomingMessageId = context.MessageId;
                testContext.IncomingMessageReceived = true;
                return context.Reply(new ReplyMessage());
            }
        }
    }

    class TestEndpoint : EndpointConfigurationBuilder
    {
        public TestEndpoint() => EndpointSetup<OpenTelemetryEnabledEndpoint>();

        class MessageHandler : IHandleMessages<ReplyMessage>
        {
            Context testContext;

            public MessageHandler(Context testContext) => this.testContext = testContext;

            public Task Handle(ReplyMessage message, IMessageHandlerContext context)
            {
                testContext.ReplyMessageId = context.MessageId;
                testContext.ReplyMessageReceived = true;
                return Task.CompletedTask;
            }
        }
    }

    public class RetryEndpoint : EndpointConfigurationBuilder
    {
        public RetryEndpoint()
        {
            EndpointSetup<DefaultServer, Context>((config, context) =>
            {
                var recoverability = config.Recoverability();
                recoverability.Delayed(settings => settings.NumberOfRetries(1).TimeIncrease(TimeSpan.FromMilliseconds(1)));
            });
        }

        class MessageToBeRetriedHandler : IHandleMessages<MessageToBeRetried>
        {
            public Task Handle(MessageToBeRetried message, IMessageHandlerContext context)
            {
                throw new SimulatedException();
            }
        }
    }


    public class MessageToBeRetried : IMessage
    {
    }

    public class IncomingMessage : IMessage
    {
    }

    public class ReplyMessage : IMessage
    {
    }
}