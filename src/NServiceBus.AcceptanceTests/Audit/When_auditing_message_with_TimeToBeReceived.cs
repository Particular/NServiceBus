namespace NServiceBus.AcceptanceTests.Audit;

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
            .Run();

        Assert.That(context.IsMessageHandlingComplete, Is.True);
    }

    public class Context : ScenarioContext
    {
        public int AuditRetries;
        public bool IsMessageHandlingComplete { get; set; }
    }

    public class EndpointWithAuditOn : EndpointConfigurationBuilder
    {
        public EndpointWithAuditOn() => EndpointSetup<DefaultServer>(c => c.AuditProcessedMessagesTo<EndpointThatHandlesAuditMessages>());

        [Handler]
        public class MessageToBeAuditedHandler(Context testContext) : IHandleMessages<MessageToBeAudited>
        {
            public Task Handle(MessageToBeAudited message, IMessageHandlerContext context)
            {
                testContext.IsMessageHandlingComplete = true;
                return Task.CompletedTask;
            }
        }
    }

    public class EndpointThatHandlesAuditMessages : EndpointConfigurationBuilder
    {
        public EndpointThatHandlesAuditMessages() => EndpointSetup<DefaultServer>(c => c.Recoverability().Immediate(s => s.NumberOfRetries(10)));

        [Handler]
        public class AuditMessageHandler(Context textContext) : IHandleMessages<MessageToBeAudited>
        {
            public async Task Handle(MessageToBeAudited message, IMessageHandlerContext context)
            {
                if (textContext.AuditRetries > 0)
                {
                    textContext.MarkAsCompleted();
                    return;
                }

                var ttbr = TimeSpan.Parse(context.MessageHeaders[Headers.TimeToBeReceived]);
                // wait longer than configured TTBR
                await Task.Delay(ttbr.Add(TimeSpan.FromSeconds(1)));

                // enforce message retry
                Interlocked.Increment(ref textContext.AuditRetries);
                throw new Exception("retry message");
            }
        }
    }

    [TimeToBeReceived("00:00:03")]
    public class MessageToBeAudited : IMessage;
}