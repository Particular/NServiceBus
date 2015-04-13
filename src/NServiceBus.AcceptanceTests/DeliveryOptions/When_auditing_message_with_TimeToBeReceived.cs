namespace NServiceBus.AcceptanceTests.Audit
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_auditing_message_with_TimeToBeReceived : NServiceBusAcceptanceTest
    {

        [Test]
        public void Should_not_honor_TimeToBeReceived_for_audit_message()
        {
            var context = new Context();
            Scenario.Define(context)
            .WithEndpoint<EndpointWithAuditOn>(b => b.Given(Send()))
            .WithEndpoint<EndpointThatHandlesAuditMessages>()
            .Done(c => c.IsMessageHandlingComplete && context.TTBRHasExpiredAndMessageIsStillInAuditQueue)
            .Run();
            Assert.IsTrue(context.IsMessageHandlingComplete);
        }

        static Action<IBus> Send()
        {
            return bus => bus.SendLocal(new MessageToBeAudited());
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
                Context context;

                public MessageToBeAuditedHandler(Context context)
                {
                    this.context = context;
                }

                public void Handle(MessageToBeAudited message)
                {
                    context.IsMessageHandlingComplete = true;
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
                Context context;
                IBus bus;

                public AuditMessageHandler(Context context, IBus bus)
                {
                    this.context = context;
                    this.bus = bus;
                }

                public void Handle(MessageToBeAudited message)
                {
                    var auditProcessingStarted = DateTime.Now;
                    if (context.FirstTimeProcessedByAudit == null)
                    {
                        context.FirstTimeProcessedByAudit = auditProcessingStarted;
                    }

                    var ttbr = TimeSpan.Parse(bus.CurrentMessageContext.Headers[Headers.TimeToBeReceived]);
                    var ttbrExpired = auditProcessingStarted > (context.FirstTimeProcessedByAudit.Value + ttbr);
                    if (ttbrExpired)
                    {
                        context.TTBRHasExpiredAndMessageIsStillInAuditQueue = true;
                        var timeElapsedSinceFirstHandlingOfAuditMessage = auditProcessingStarted - context.FirstTimeProcessedByAudit.Value;
                        Console.WriteLine("Audit message not removed because of TTBR({0}) after {1}. Success.", ttbr, timeElapsedSinceFirstHandlingOfAuditMessage);
                    }
                    else
                    {
                        bus.HandleCurrentMessageLater();
                    }
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
