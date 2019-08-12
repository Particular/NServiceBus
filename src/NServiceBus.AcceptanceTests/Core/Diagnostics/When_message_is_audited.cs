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
            var now = DateTime.UtcNow;

            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithAuditOn>(b => b.When(session => session.SendLocal(new MessageToBeAudited())))
                .WithEndpoint<EndpointThatHandlesAuditMessages>()
                .Done(c => c.IsMessageHandledByTheAuditEndpoint)
                .Run();

            var processingStarted = DateTimeExtensions.ToUtcDateTime(context.Headers[Headers.ProcessingStarted]);
            var processingEnded = DateTimeExtensions.ToUtcDateTime(context.Headers[Headers.ProcessingEnded]);
            var timeSent = DateTimeExtensions.ToUtcDateTime(context.Headers[Headers.TimeSent]);

            Assert.That(processingStarted, Is.EqualTo(now).Within(TimeSpan.FromSeconds(30)));
            Assert.That(processingEnded, Is.EqualTo(now).Within(TimeSpan.FromSeconds(30)));
            Assert.That(timeSent, Is.EqualTo(now).Within(TimeSpan.FromSeconds(30)));
            Assert.That(processingStarted, Is.LessThanOrEqualTo(processingEnded));
            Assert.That(timeSent, Is.LessThanOrEqualTo(processingEnded));
            Assert.IsTrue(context.IsMessageHandledByTheAuditEndpoint);
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