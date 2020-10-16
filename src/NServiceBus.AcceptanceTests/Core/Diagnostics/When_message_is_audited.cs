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

    public class When_message_is_audited : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_contain_processing_stats_headers()
        {
            var now = DateTimeOffset.UtcNow;

            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithAuditOn>(b => b.When(session => session.SendLocal(new MessageToBeAudited())))
                .WithEndpoint<EndpointThatHandlesAuditMessages>()
                .Done(c => c.IsMessageHandledByTheAuditEndpoint)
                .Run();

            var processingStarted = DateTimeOffsetHelper.ToDateTimeOffset(context.Headers[Headers.ProcessingStarted]);
            var processingEnded = DateTimeOffsetHelper.ToDateTimeOffset(context.Headers[Headers.ProcessingEnded]);
            var timeSent = DateTimeOffsetHelper.ToDateTimeOffset(context.Headers[Headers.TimeSent]);

            Assert.That(processingStarted, Is.EqualTo(now).Within(TimeSpan.FromSeconds(30)), nameof(processingStarted));
            Assert.That(processingEnded, Is.EqualTo(now).Within(TimeSpan.FromSeconds(30)), nameof(processingEnded));
            Assert.That(timeSent, Is.EqualTo(now).Within(TimeSpan.FromSeconds(30)), nameof(timeSent));
            Assert.That(processingStarted, Is.LessThanOrEqualTo(processingEnded), nameof(processingStarted));
            Assert.That(timeSent, Is.LessThanOrEqualTo(processingEnded), nameof(timeSent));
            Assert.IsTrue(context.IsMessageHandledByTheAuditEndpoint, nameof(context.IsMessageHandledByTheAuditEndpoint));
        }

        public class Context : ScenarioContext
        {
            public bool IsMessageHandledByTheAuditEndpoint { get; set; }
            public IReadOnlyDictionary<string, string> Headers { get; set; }
        }

        public class EndpointWithAuditOn : EndpointConfigurationBuilder
        {
            public EndpointWithAuditOn()
            {
                EndpointSetup<DefaultServer>(c => c
                    .AuditProcessedMessagesTo<EndpointThatHandlesAuditMessages>());
            }

            class MessageToBeAuditedHandler : IHandleMessages<MessageToBeAudited>
            {
                public Task Handle(MessageToBeAudited message, IMessageHandlerContext context1)
                {
                    return Task.FromResult(0);
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
                    testContext.Headers = context.MessageHeaders.ToDictionary(x => x.Key, x => x.Value);
                    testContext.IsMessageHandledByTheAuditEndpoint = true;
                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        public class MessageToBeAudited : IMessage
        {
        }
    }
}