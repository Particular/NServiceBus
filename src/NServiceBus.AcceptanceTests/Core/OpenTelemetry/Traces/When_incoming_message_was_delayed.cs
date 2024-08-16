namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Traces;

using System;
using System.Diagnostics;
using System.Linq;
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
        Assert.That(incomingMessageActivities.Count, Is.EqualTo(2), "2 messages are received as part of this test");
        Assert.That(outgoingMessageActivities.Count, Is.EqualTo(2), "2 messages are sent as part of this test");

        var sendRequest = outgoingMessageActivities[0];
        var receiveRequest = incomingMessageActivities[0];
        var sendReply = outgoingMessageActivities[1];
        var receiveReply = incomingMessageActivities[1];

        Assert.AreNotEqual(sendRequest.RootId, receiveRequest.RootId, "send and receive operations are part of different root activities");
        Assert.IsNull(receiveRequest.ParentId, "first incoming message does not have a parent, it's a root");
        Assert.AreNotEqual(sendRequest.RootId, sendReply.RootId, "first send operation is different than the root activity of the reply");
        Assert.That(receiveReply.ParentId, Is.EqualTo(sendReply.Id), "second incoming message is correlated to the second send operation");
        Assert.That(receiveReply.RootId, Is.EqualTo(sendReply.RootId), "second incoming message is the root activity");

        ActivityLink link = receiveRequest.Links.FirstOrDefault();
        Assert.IsNotNull(link, "second receive has a link");
        Assert.That(link.Context.TraceId, Is.EqualTo(sendRequest.TraceId), "second receive is linked to send operation");
    }

    [Test]
    public async Task By_retry_Should_create_new_trace_and_link_to_send()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<RetryEndpoint>(b => b
                .CustomConfig(c => c.ConfigureRouting().RouteToEndpoint(typeof(IncomingMessage), typeof(ReplyingEndpoint)))
                .When(session => session.SendLocal(new MessageToBeRetried()))
                .DoNotFailOnErrorMessages())
            .Done(c => !c.FailedMessages.IsEmpty)
            .Run(TimeSpan.FromSeconds(120));

        var incomingMessageActivities = NServicebusActivityListener.CompletedActivities.GetReceiveMessageActivities();
        var outgoingMessageActivities = NServicebusActivityListener.CompletedActivities.GetSendMessageActivities();
        Assert.That(incomingMessageActivities.Count, Is.EqualTo(2), "2 messages are received as part of this test (2 attempts)");
        Assert.That(outgoingMessageActivities.Count, Is.EqualTo(1), "1 message sent as part of this test");

        var sendRequest = outgoingMessageActivities[0];
        var firstAttemptReceiveRequest = incomingMessageActivities[0];
        var secondAttemptReceiveRequest = incomingMessageActivities[1];

        Assert.That(firstAttemptReceiveRequest.RootId, Is.EqualTo(sendRequest.RootId), "first send operation is the root activity");
        Assert.That(firstAttemptReceiveRequest.ParentId, Is.EqualTo(sendRequest.Id), "first incoming message is correlated to the first send operation");

        Assert.AreNotEqual(sendRequest.RootId, secondAttemptReceiveRequest.RootId, "send and 2nd receive operations are part of different root activities");
        Assert.IsNull(secondAttemptReceiveRequest.ParentId, "first incoming message does not have a parent, it's a root");
        ActivityLink link = secondAttemptReceiveRequest.Links.FirstOrDefault();
        Assert.IsNotNull(link, "second receive has a link");
        Assert.That(link.Context.TraceId, Is.EqualTo(sendRequest.TraceId), "second receive is linked to send operation");
    }

    [Test]
    public async Task By_saga_timeout_Should_create_new_trace_and_link_to_send()
    {
        var context = await Scenario.Define<SagaContext>()
            .WithEndpoint<SagaOtelEndpoint>(b => b
                .When(s => s.SendLocal(new StartSagaMessage { SomeId = Guid.NewGuid().ToString() })))
            .Done(c => c.SagaMarkedComplete)
            .Run();

        var incomingMessageActivities = NServicebusActivityListener.CompletedActivities.GetReceiveMessageActivities();
        var outgoingMessageActivities = NServicebusActivityListener.CompletedActivities.GetSendMessageActivities();
        Assert.That(incomingMessageActivities.Count, Is.EqualTo(3), "3 messages are received as part of this test");
        Assert.That(outgoingMessageActivities.Count, Is.EqualTo(3), "3 messages are sent as part of this test");

        var startSagaSend = outgoingMessageActivities[0];
        var startSagaReceive = incomingMessageActivities[0];
        var timeoutSend = outgoingMessageActivities[1];
        var timeoutReceive = incomingMessageActivities[1];
        var completeSagaSend = outgoingMessageActivities[2];
        var completeSagaReceive = incomingMessageActivities[2];

        Assert.That(startSagaReceive.RootId, Is.EqualTo(startSagaSend.RootId), "send start saga operation is the root activity of the receive start saga operation");
        Assert.That(startSagaReceive.ParentId, Is.EqualTo(startSagaSend.Id), "start saga receive operation is child of the start saga send operation");
        Assert.That(timeoutSend.RootId, Is.EqualTo(startSagaSend.RootId), "timeout send operation is part of the start saga operation root");

        Assert.AreNotEqual(timeoutSend.RootId, timeoutReceive.RootId, "timeout send and receive operations are part of different root activities");
        Assert.IsNull(timeoutReceive.ParentId, "timeout receive operation does not have a parent, it's a root");
        ActivityLink timeoutReceiveLink = timeoutReceive.Links.FirstOrDefault();
        Assert.IsNotNull(timeoutReceiveLink, "timeout receive operation is linked");
        Assert.That(timeoutReceiveLink.Context.TraceId, Is.EqualTo(timeoutSend.TraceId), "imeout receive operation links to the timeout send operation");

        Assert.That(completeSagaSend.RootId, Is.EqualTo(timeoutReceive.RootId), "timeout receive operation is the root of the complete saga send operation");
        Assert.That(completeSagaReceive.RootId, Is.EqualTo(timeoutReceive.RootId), "timeout receive operation is the root of the complete saga receive operation");
        Assert.That(completeSagaReceive.ParentId, Is.EqualTo(completeSagaSend.Id), "complete saga send operation is the parent of the complete saga receive operation");
    }

    class Context : ScenarioContext
    {
        public bool ReplyMessageReceived { get; set; }
        public string IncomingMessageId { get; set; }
        public string ReplyMessageId { get; set; }
        public bool IncomingMessageReceived { get; set; }
    }
    class SagaContext : ScenarioContext
    {
        public bool SagaStarted { get; set; }
        public bool TimeoutReceived { get; set; }
        public bool SagaMarkedComplete { get; set; }
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
        public TestEndpoint()
        {
            var template = new DefaultServer
            {
                TransportConfiguration = new ConfigureEndpointAcceptanceTestingTransport(false, true)
            };
            EndpointSetup(
                template,
                (c, _) =>
                {
                    c.EnableOpenTelemetry();
                    var recoverability = c.Recoverability();
                    recoverability.Delayed(settings => settings.NumberOfRetries(1).TimeIncrease(TimeSpan.FromMilliseconds(1)));
                }, metadata => { });
        }

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

    class SagaOtelEndpoint : EndpointConfigurationBuilder
    {
        public SagaOtelEndpoint()
        {
            var template = new DefaultServer
            {
                TransportConfiguration = new ConfigureEndpointAcceptanceTestingTransport(false, true)
            };
            EndpointSetup(
                template,
                (c, _) =>
                {
                    c.EnableOpenTelemetry();
                    var recoverability = c.Recoverability();
                    recoverability.Delayed(settings => settings.NumberOfRetries(1).TimeIncrease(TimeSpan.FromMilliseconds(1)));
                }, metadata => { });
        }

        public class OtelSaga : Saga<MyOtelSagaData>, IAmStartedByMessages<StartSagaMessage>, IHandleTimeouts<TimeoutMessage>, IHandleMessages<CompleteSagaMessage>
        {
            SagaContext testContext;

            public OtelSaga(SagaContext testContext) => this.testContext = testContext;

            public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
            {
                Data.SomeId = message.SomeId;
                testContext.SagaStarted = true;
                return RequestTimeout<TimeoutMessage>(context, DateTimeOffset.UtcNow.AddMilliseconds(2));
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyOtelSagaData> mapper)
            {
                mapper.ConfigureMapping<StartSagaMessage>(m => m.SomeId).ToSaga(s => s.SomeId);
                mapper.ConfigureMapping<CompleteSagaMessage>(m => m.SomeId).ToSaga(s => s.SomeId);
            }
            public Task Timeout(TimeoutMessage state, IMessageHandlerContext context)
            {
                testContext.TimeoutReceived = true;
                return context.SendLocal(new CompleteSagaMessage { SomeId = Data.SomeId });
            }

            public Task Handle(CompleteSagaMessage message, IMessageHandlerContext context)
            {
                MarkAsComplete();
                testContext.SagaMarkedComplete = true;
                return Task.CompletedTask;
            }
        }
        public class MyOtelSagaData : ContainSagaData
        {
            public virtual string SomeId { get; set; }
        }
    }

    public class RetryEndpoint : EndpointConfigurationBuilder
    {
        public RetryEndpoint()
        {
            var template = new DefaultServer
            {
                TransportConfiguration = new ConfigureEndpointAcceptanceTestingTransport(false, true)
            };
            EndpointSetup(
                template,
                (c, _) =>
                {
                    c.EnableOpenTelemetry();
                    var recoverability = c.Recoverability();
                    recoverability.Delayed(settings => settings.NumberOfRetries(1).TimeIncrease(TimeSpan.FromMilliseconds(1)));
                }, metadata => { });
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
    public class StartSagaMessage : IMessage
    {
        public string SomeId { get; set; }
    }
    public class TimeoutMessage { }
    public class CompleteSagaMessage : IMessage
    {
        public string SomeId { get; set; }
    }

    public class ReplyMessage : IMessage
    {
    }
}