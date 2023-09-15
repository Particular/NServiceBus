﻿namespace NServiceBus.AcceptanceTests.Recoverability
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using AcceptanceTesting.EndpointTemplates;
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
                .Done(c => c.ConfirmedRetryId != null && c.AuditHeaders != null)
                .Run();

            Assert.IsTrue(context.MessageProcessed);
            Assert.AreEqual(context.RetryId, context.ConfirmedRetryId);
            var processingTime = DateTimeOffsetHelper.ToDateTimeOffset(context.RetryProcessingTimestamp);
            Assert.That(processingTime, Is.EqualTo(DateTimeOffset.UtcNow).Within(TimeSpan.FromMinutes(1)));
            Assert.IsTrue(context.AuditHeaders.ContainsKey("ServiceControl.Retry.AcknowledgementSent"));
        }

        class Context : ScenarioContext
        {
            public string RetryId { get; set; }
            public string ConfirmedRetryId { get; set; }
            public string RetryProcessingTimestamp { get; set; }
            public bool MessageProcessed { get; set; }
            public IReadOnlyDictionary<string, string> AuditHeaders { get; set; }
        }

        class ProcessingEndpoint : EndpointConfigurationBuilder
        {
            public ProcessingEndpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.AuditProcessedMessagesTo<AuditSpy>();
                });
            }

            class FailedMessageHandler : IHandleMessages<FailedMessage>
            {
                Context testContext;

                public FailedMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

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

            class ControlMessageBehavior : Behavior<IIncomingPhysicalMessageContext>
            {
                Context testContext;

                public ControlMessageBehavior(Context testContext)
                {
                    this.testContext = testContext;
                }

                public override async Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
                {
                    await next();

                    testContext.ConfirmedRetryId = context.MessageHeaders["ServiceControl.Retry.UniqueMessageId"];
                    testContext.RetryProcessingTimestamp = context.MessageHeaders["ServiceControl.Retry.Successful"];
                }
            }
        }

        class AuditSpy : EndpointConfigurationBuilder
        {
            public AuditSpy() => EndpointSetup<DefaultServer>();

            class FailedMessageHandler : IHandleMessages<FailedMessage>
            {
                Context testContext;

                public FailedMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(FailedMessage message, IMessageHandlerContext context)
                {
                    testContext.AuditHeaders = context.MessageHeaders;
                    return Task.CompletedTask;
                }
            }
        }

        public class FailedMessage : IMessage
        {
        }
    }
}