namespace NServiceBus.AcceptanceTests.Core.Diagnostics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_message_is_faulted : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_contain_processing_stats_headers()
        {
            var now = DateTimeOffset.UtcNow;

            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithAuditOn>(b => b.When(session => session.SendLocal(new MessageToBeAudited())).DoNotFailOnErrorMessages())
                .WithEndpoint<EndpointThatHandlesAuditMessages>()
                .WithEndpoint<EndpointThatHandlesErrorMessages>()
                .Done(c => c.IsMessageHandledByTheAuditEndpoint && c.IsMessageHandledByTheFaultEndpoint)
                .Run();

            var processingStarted = DateTimeOffsetHelper.ToDateTimeOffset(context.Headers[Headers.ProcessingStarted]);
            var processingEnded = DateTimeOffsetHelper.ToDateTimeOffset(context.Headers[Headers.ProcessingEnded]);
            var timeSent = DateTimeOffsetHelper.ToDateTimeOffset(context.Headers[Headers.TimeSent]);
            var timeSentWhenFailedMessageWasSentToTheErrorQueue = DateTimeOffsetHelper.ToDateTimeOffset(context.FaultHeaders[Headers.TimeSent]);

            Assert.That(processingStarted, Is.EqualTo(now).Within(TimeSpan.FromSeconds(30)), nameof(processingStarted));
            Assert.That(processingEnded, Is.EqualTo(now).Within(TimeSpan.FromSeconds(30)), nameof(processingEnded));
            Assert.That(timeSent, Is.EqualTo(now).Within(TimeSpan.FromSeconds(30)), nameof(timeSent));
            Assert.That(timeSentWhenFailedMessageWasSentToTheErrorQueue, Is.EqualTo(now).Within(TimeSpan.FromSeconds(30)), nameof(timeSentWhenFailedMessageWasSentToTheErrorQueue));
            Assert.That(timeSent, Is.LessThanOrEqualTo(processingEnded), nameof(processingEnded));
            Assert.That(timeSent, Is.LessThanOrEqualTo(timeSentWhenFailedMessageWasSentToTheErrorQueue), nameof(timeSentWhenFailedMessageWasSentToTheErrorQueue));

            Assert.That(timeSentWhenFailedMessageWasSentToTheErrorQueue, Is.EqualTo(context.TimeSentOnTheFailingMessageWhenItWasHandled), nameof(timeSentWhenFailedMessageWasSentToTheErrorQueue));
            Assert.That(processingStarted, Is.LessThanOrEqualTo(processingEnded), nameof(processingStarted));
            Assert.IsTrue(context.IsMessageHandledByTheFaultEndpoint, nameof(context.IsMessageHandledByTheFaultEndpoint));
        }

        public class Context : ScenarioContext
        {
            public bool IsMessageHandledByTheAuditEndpoint { get; set; }
            public bool IsMessageHandledByTheFaultEndpoint { get; set; }
            public IDictionary<string, string> Headers { get; set; }
            public IReadOnlyDictionary<string, string> FaultHeaders { get; set; }
            public DateTimeOffset TimeSentOnTheFailingMessageWhenItWasHandled { get; set; }
        }

        public class EndpointWithAuditOn : EndpointConfigurationBuilder
        {
            public EndpointWithAuditOn()
            {
                EndpointSetup<DefaultServer>((c, r) =>
                {
                    c.SendFailedMessagesTo<EndpointThatHandlesErrorMessages>();
                    c.AuditProcessedMessagesTo<EndpointThatHandlesAuditMessages>();
                });
            }

            class MessageToBeAuditedHandler : IHandleMessages<MessageToBeAudited>
            {
                public Task Handle(MessageToBeAudited message, IMessageHandlerContext context)
                {
                    return context.SendLocal(new MessageThatFails());
                }

                public class MessageSentInsideHandlersHandler : IHandleMessages<MessageThatFails>
                {
                    public Context TestContext { get; set; }

                    public Task Handle(MessageThatFails message, IMessageHandlerContext context)
                    {
                        throw new SimulatedException();
                    }
                }
            }
        }

        public class EndpointThatHandlesAuditMessages : EndpointConfigurationBuilder
        {
            public EndpointThatHandlesAuditMessages()
            {
                EndpointSetup<DefaultServer>();
            }

            class AuditMessageHandler : IHandleMessages<MessageToBeAudited>
            {
                public AuditMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(MessageToBeAudited message, IMessageHandlerContext context)
                {
                    testContext.Headers = context.MessageHeaders;
                    testContext.IsMessageHandledByTheAuditEndpoint = true;
                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        class EndpointThatHandlesErrorMessages : EndpointConfigurationBuilder
        {
            public EndpointThatHandlesErrorMessages()
            {
                EndpointSetup<DefaultServer>();
            }

            class ErrorMessageHandler : IHandleMessages<MessageThatFails>
            {
                public ErrorMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(MessageThatFails message, IMessageHandlerContext context)
                {
                    testContext.TimeSentOnTheFailingMessageWhenItWasHandled = DateTimeOffsetHelper.ToDateTimeOffset(context.MessageHeaders[Headers.TimeSent]);
                    testContext.FaultHeaders = context.MessageHeaders.ToDictionary(x => x.Key, x => x.Value);
                    testContext.IsMessageHandledByTheFaultEndpoint = true;

                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        public class MessageToBeAudited : IMessage
        {
        }

        public class MessageThatFails : IMessage
        {
        }
    }
}