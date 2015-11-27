namespace NServiceBus.AcceptanceTests.Audit
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_auditing_message_with_TimeToBeReceived : NServiceBusAcceptanceTest
    {

        [Test]
        public async Task Should_not_honor_TimeToBeReceived_for_audit_message()
        {
            var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithAuditOn>(b => b.When(bus => bus.SendLocal(new MessageToBeAudited())))
            .WithEndpoint<EndpointThatHandlesAuditMessages>()
            .Done(c => c.IsMessageHandlingComplete && c.TTBRHasExpiredAndMessageIsStillInAuditQueue)
            .Run();

            Assert.IsTrue(context.IsMessageHandlingComplete);
        }

        class Context : ScenarioContext
        {
            public bool IsMessageHandlingComplete { get; set; }
            public DateTime? FirstTimeProcessedByAudit { get; set; }
            public bool TTBRHasExpiredAndMessageIsStillInAuditQueue { get; set; }
        }

        class EndpointWithAuditOn : EndpointConfigurationBuilder
        {

            public EndpointWithAuditOn()
            {
                EndpointSetup<DefaultServer>()
                    .AuditTo<EndpointThatHandlesAuditMessages>();
            }

            class MessageToBeAuditedHandler : IHandleMessages<MessageToBeAudited>
            {
                Context testContext;

                public MessageToBeAuditedHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(MessageToBeAudited message, IMessageHandlerContext context)
                {
                    testContext.IsMessageHandlingComplete = true;
                    return Task.FromResult(0);
                }
            }
        }

        class EndpointThatHandlesAuditMessages : EndpointConfigurationBuilder
        {

            public EndpointThatHandlesAuditMessages()
            {
                EndpointSetup<DefaultServer>();
            }

            class AuditMessageHandler : IHandleMessages<MessageToBeAudited>
            {
                Context textContext;

                public AuditMessageHandler(Context textContext)
                {
                    this.textContext = textContext;
                }

                public Task Handle(MessageToBeAudited message, IMessageHandlerContext context)
                {
                    var auditProcessingStarted = DateTime.Now;
                    if (textContext.FirstTimeProcessedByAudit == null)
                    {
                        textContext.FirstTimeProcessedByAudit = auditProcessingStarted;
                    }

                    var ttbr = TimeSpan.Parse(context.MessageHeaders[Headers.TimeToBeReceived]);
                    var ttbrExpired = auditProcessingStarted > textContext.FirstTimeProcessedByAudit.Value + ttbr;
                    if (ttbrExpired)
                    {
                        textContext.TTBRHasExpiredAndMessageIsStillInAuditQueue = true;
                        var timeElapsedSinceFirstHandlingOfAuditMessage = auditProcessingStarted - textContext.FirstTimeProcessedByAudit.Value;
                        Console.WriteLine("Audit message not removed because of TTBR({0}) after {1}. Succeeded.", ttbr, timeElapsedSinceFirstHandlingOfAuditMessage);
                    }
                    else
                    {
                        return context.HandleCurrentMessageLater();
                    }

                    return Task.FromResult(0);
                }
            }
        }

        [Serializable]
        [TimeToBeReceived("00:00:03")]
        class MessageToBeAudited : IMessage
        {
        }
    }
}
