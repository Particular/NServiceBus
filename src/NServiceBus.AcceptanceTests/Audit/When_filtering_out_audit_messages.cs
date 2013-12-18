
namespace NServiceBus.AcceptanceTests.Audit
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Audit;
    using NServiceBus.Audit.Config;
    using NUnit.Framework;

    public class When_filtering_out_audit_messages : NServiceBusAcceptanceTest
    {
        [Test]
        public void Message_should_not_be_forwarded_to_auditQueue_when_auditing_is_disabled()
        {
            var context = new Context();
            Scenario.Define(context)
            .WithEndpoint<EndpointWithCustomAudit>(b => b.Given(bus => bus.SendLocal(new MessageToBeAudited())))
            .WithEndpoint<EndpointThatHandlesAuditMessages>()
            .Done(c => c.IsMessageHandlingComplete)
            .Run();

            Assert.IsFalse(context.IsMessageHandledByTheAuditEndpoint);
        }

        public class Context : ScenarioContext
        {
            public bool IsMessageHandlingComplete { get; set; }
            public bool IsMessageHandledByTheAuditEndpoint { get; set; }
        }

        public class EndpointWithCustomAudit : EndpointConfigurationBuilder
        {
            public EndpointWithCustomAudit()
            {
                EndpointSetup<DefaultServer>(c => Configure.Features.Audit(f => f.ExcludeMessageTypeFromAudit(typeof(MessageToBeAudited))))
                    .AuditTo<EndpointThatHandlesAuditMessages>();
            }

            class MessageToBeAuditedHandler : IHandleMessages<MessageToBeAudited>
            {
                public Context MyContext { get; set; }

                public void Handle(MessageToBeAudited message)
                {
                    MyContext.IsMessageHandlingComplete = true;
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
                public Context MyContext { get; set; }

                public void Handle(MessageToBeAudited message)
                {
                    MyContext.IsMessageHandledByTheAuditEndpoint = true;
                }
            }
        }

        [Serializable]
        public class MessageToBeAudited : IMessage
        {
        }
    }
}
