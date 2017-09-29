namespace NServiceBus.AcceptanceTests.Audit
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_auditing_message_with_TimeToBeReceived : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_honor_TimeToBeReceived_for_audit_message()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithAuditOn>(b => b.When(session => session.SendLocal(new MessageToBeAudited())))
                .WithEndpoint<EndpointThatHandlesAuditMessages>()
                .Done(c => c.IsMessageHandlingComplete && c.TTBRHasExpiredAndMessageIsStillInAuditQueue)
                .Run();

            Assert.IsTrue(context.IsMessageHandlingComplete);
        }

        class Context : ScenarioContext
        {
            public int AuditRetries;
            public bool IsMessageHandlingComplete { get; set; }
            public bool TTBRHasExpiredAndMessageIsStillInAuditQueue { get; set; }
        }

        class EndpointWithAuditOn : EndpointConfigurationBuilder
        {
            public EndpointWithAuditOn()
            {
                EndpointSetup<DefaultServer>(c => c.AuditProcessedMessagesTo<EndpointThatHandlesAuditMessages>());
            }

            class MessageToBeAuditedHandler : IHandleMessages<MessageToBeAudited>
            {
                public MessageToBeAuditedHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(MessageToBeAudited message, IMessageHandlerContext context)
                {
                    testContext.IsMessageHandlingComplete = true;
                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        class EndpointThatHandlesAuditMessages : EndpointConfigurationBuilder
        {
            public EndpointThatHandlesAuditMessages()
            {
                EndpointSetup<DefaultServer>(c => c.Recoverability().Immediate(s => s.NumberOfRetries(10)));
            }

            class AuditMessageHandler : IHandleMessages<MessageToBeAudited>
            {
                public AuditMessageHandler(Context textContext)
                {
                    this.textContext = textContext;
                }

                public async Task Handle(MessageToBeAudited message, IMessageHandlerContext context)
                {
                    if (textContext.AuditRetries > 0)
                    {
                        textContext.TTBRHasExpiredAndMessageIsStillInAuditQueue = true;
                        return;
                    }

                    var ttbr = TimeSpan.Parse(context.MessageHeaders[Headers.TimeToBeReceived]);
                    // wait longer than configured TTBR
                    await Task.Delay(ttbr.Add(TimeSpan.FromSeconds(1)));

                    // enforce message retry
                    Interlocked.Increment(ref textContext.AuditRetries);
                    throw new Exception("retry message");
                }

                Context textContext;
            }
        }

        [TimeToBeReceived("00:00:03")]
        public class MessageToBeAudited : IMessage
        {
        }
    }
}