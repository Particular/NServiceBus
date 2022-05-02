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
        // This test has repeatedly failed because the message took longer than the TTBR value to be received.
        // We assume this could be due to the parallel test execution.
        // If this test fails your build with this attribute set, please ping the NServiceBus maintainers.
        [NonParallelizable]
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
                public Task Handle(MessageToBeAudited message, IMessageHandlerContext context)
                {
                    var testContext = context.GetTestContext<Context>();
                    testContext.IsMessageHandlingComplete = true;
                    return Task.FromResult(0);
                }
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
                public async Task Handle(MessageToBeAudited message, IMessageHandlerContext context)
                {
                    var testContext = context.GetTestContext<Context>();

                    if (testContext.AuditRetries > 0)
                    {
                        testContext.TTBRHasExpiredAndMessageIsStillInAuditQueue = true;
                        return;
                    }

                    var ttbr = TimeSpan.Parse(context.MessageHeaders[Headers.TimeToBeReceived]);
                    // wait longer than configured TTBR
                    await Task.Delay(ttbr.Add(TimeSpan.FromSeconds(1)));

                    // enforce message retry
                    Interlocked.Increment(ref testContext.AuditRetries);
                    throw new Exception("retry message");
                }
            }
        }

        [TimeToBeReceived("00:00:03")]
        public class MessageToBeAudited : IMessage
        {
        }
    }
}