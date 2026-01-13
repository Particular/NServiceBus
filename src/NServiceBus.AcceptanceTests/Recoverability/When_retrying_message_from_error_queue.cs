namespace NServiceBus.AcceptanceTests.Recoverability;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using EndpointTemplates;
using NServiceBus.Pipeline;
using NUnit.Framework;

public class When_retrying_message_from_error_queue : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_confirm_successful_processing()
    {
        var context = await Scenario.Define<Context>(c => c.RetryId = Guid.NewGuid().ToString("D"))
            .WithEndpoint<ProcessingEndpoint>(e => e
                .When((s, ctx) =>
                {
                    var sendOptions = new SendOptions();
                    sendOptions.RouteToThisEndpoint();
                    // set SC retry header information
                    sendOptions.SetHeader("ServiceControl.Retry.UniqueMessageId", ctx.RetryId);
                    sendOptions.SetHeader("ServiceControl.Retry.AcknowledgementQueue", Conventions.EndpointNamingConvention(typeof(RetryAckSpy)));
                    return s.Send(new FailedMessage(), sendOptions);
                }))
            .WithEndpoint<RetryAckSpy>()
            .WithEndpoint<AuditSpy>()
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.MessageProcessed, Is.True);
            Assert.That(context.ConfirmedRetryId, Is.EqualTo(context.RetryId));
        }
        var processingTime = DateTimeOffsetHelper.ToDateTimeOffset(context.RetryProcessingTimestamp);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(processingTime, Is.EqualTo(DateTimeOffset.UtcNow).Within(TimeSpan.FromMinutes(1)));
            Assert.That(context.AuditHeaders.ContainsKey("ServiceControl.Retry.AcknowledgementSent"), Is.True);
        }
    }

    class Context : ScenarioContext
    {
        public string RetryId { get; set; }
        public string ConfirmedRetryId { get; set; }
        public string RetryProcessingTimestamp { get; set; }
        public bool MessageProcessed { get; set; }
        public IReadOnlyDictionary<string, string> AuditHeaders { get; set; }

        public void MaybeCompleted() => MarkAsCompleted(ConfirmedRetryId != null, AuditHeaders != null);
    }

    class ProcessingEndpoint : EndpointConfigurationBuilder
    {
        public ProcessingEndpoint() =>
            EndpointSetup<DefaultServer>(c =>
            {
                c.AuditProcessedMessagesTo<AuditSpy>();
            });

        class FailedMessageHandler(Context testContext) : IHandleMessages<FailedMessage>
        {
            public Task Handle(FailedMessage message, IMessageHandlerContext context)
            {
                testContext.MessageProcessed = true;
                return Task.CompletedTask;
            }
        }
    }

    class RetryAckSpy : EndpointConfigurationBuilder
    {
        public RetryAckSpy() => EndpointSetup<DefaultServer>((e, r) => e.Pipeline.Register(
            new ControlMessageBehavior(r.ScenarioContext as Context),
            "Checks for confirmation control message"));

        class ControlMessageBehavior(Context testContext) : Behavior<IIncomingPhysicalMessageContext>
        {
            public override async Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
            {
                await next();

                testContext.ConfirmedRetryId = context.MessageHeaders["ServiceControl.Retry.UniqueMessageId"];
                testContext.RetryProcessingTimestamp = context.MessageHeaders["ServiceControl.Retry.Successful"];
                testContext.MaybeCompleted();
            }
        }
    }

    class AuditSpy : EndpointConfigurationBuilder
    {
        public AuditSpy() => EndpointSetup<DefaultServer>();

        class FailedMessageHandler(Context testContext) : IHandleMessages<FailedMessage>
        {
            public Task Handle(FailedMessage message, IMessageHandlerContext context)
            {
                testContext.AuditHeaders = context.MessageHeaders;
                testContext.MaybeCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class FailedMessage : IMessage;
}