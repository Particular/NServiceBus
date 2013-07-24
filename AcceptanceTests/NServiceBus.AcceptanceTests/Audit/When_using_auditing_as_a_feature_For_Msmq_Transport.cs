
using System.Linq;
using System.Messaging;

namespace NServiceBus.AcceptanceTests.Audit
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using Features;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NServiceBus.Transports.Msmq;

    public class When_using_auditing_as_a_feature_For_Msmq_Transport : NServiceBusAcceptanceTest
    {
        private const string AuditQueue = "SomeAuditQueue";
        
        [SetUp]
        public void Setup()
        {
            base.SetUp();
            
            // Check if the audit queue exists and if so, purge the messages
            if (MessageQueue.Exists(string.Format("{0}\\private$\\{1}", Environment.MachineName, AuditQueue)))
            //if (MessageQueue.Exists(string.Format(".\\{0}", AuditQueue))) -- doesn't work
            {
                var auditMsmq = new MessageQueue(MsmqUtilities.GetFullPath(Address.Parse(AuditQueue)));
                auditMsmq.Purge();
            }
        }

        [Test]
        public void Message_should_not_be_forwarded_to_auditQueue_when_auditing_is_disabled()
        {
            var context = new Context();
            Scenario.Define(context)
            .WithEndpoint<EndpointWithAuditOff>(b => b.Given(bus => bus.SendLocal(new MessageToBeAudited())))
            .Done(c => c.IsMessageHandlingComplete)
            .Run();

            Assert.IsFalse(context.IsMessageInAuditQueue);
        }

        [Test]
        public void Message_should_be_forwarded_to_auditQueue_when_auditing_is_enabled()
        {
            var context = new Context();
            Scenario.Define(context)
            .WithEndpoint<EndpointWithAuditOn>(b => b.Given(bus => bus.SendLocal(new MessageToBeAudited())))
            .Done(c => c.IsMessageHandlingComplete)
            .Run();

            Assert.IsTrue(context.IsMessageInAuditQueue);
        }

        public class Context : ScenarioContext
        {
            public bool IsMessageHandlingComplete { get; set; }

            public bool IsMessageInAuditQueue
            {
                get
                {
                    var auditMsmq = new MessageQueue(MsmqUtilities.GetFullPath(Address.Parse(AuditQueue)));
                    return auditMsmq.GetAllMessages().Any();
                }
            }
        }

        public class EndpointWithAuditOff : EndpointConfigurationBuilder
        {
           
            public EndpointWithAuditOff()
            {
                EndpointSetup<DefaultServer>(c => Configure.Features.Disable<Audit>())
                    .AuditTo(Address.Parse(AuditQueue));
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

        public class EndpointWithAuditOn : EndpointConfigurationBuilder
        {

            public EndpointWithAuditOn()
            {
                EndpointSetup<DefaultServer>()
                    .AuditTo(Address.Parse(AuditQueue));
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

        [Serializable]
        public class MessageToBeAudited : IMessage
        {
        }
    }
}
