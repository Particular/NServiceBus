
namespace NServiceBus.AcceptanceTests.Audit
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Audit;
    using NUnit.Framework;
    using Pipeline;
    using Pipeline.Contexts;

    public class When_using_a_custom_audit_behavior : NServiceBusAcceptanceTest
    {
        [Test]
        public void Excluded_types_should_not_be_audited()
        {
            var context = new Context();
            Scenario.Define(context)
            .WithEndpoint<UserEndpoint>(b => b.Given(bus => bus.SendLocal(new MessageToBeAudited())))
            .WithEndpoint<AuditSpy>()
            .Done(c => c.IsMessageHandlingComplete)
            .Run();

            Assert.IsFalse(context.MessageAudited);
        }

        public class Context : ScenarioContext
        {
            public bool IsMessageHandlingComplete { get; set; }
            public bool MessageAudited { get; set; }
        }

        public class UserEndpoint : EndpointConfigurationBuilder
        {
           
            public UserEndpoint()
            {
                EndpointSetup<DefaultServer>()
                    .AuditTo<AuditSpy>();

            }



            class MessageToBeAuditedHandler : IHandleMessages<MessageToBeAudited>
            {
                public Context MyContext { get; set; }

                public void Handle(MessageToBeAudited message)
                {
                    MyContext.IsMessageHandlingComplete = true;
                }
            }

#pragma warning disable 618
            class MyCustomAuditBehavior : PipelineOverride, IBehavior<ReceivePhysicalMessageContext>,IBehavior<ReceiveLogicalMessageContext>
            {
                public MessageAuditer MessageAuditer { get; set; }

                public void Invoke(ReceivePhysicalMessageContext context, Action next)
                {
                    var auditResult = new AuditFilterResult();
                    context.Set(auditResult);
                    next();

                    //note: andy rule operating on the raw TransportMessage can be applied here if needed.
                    // Access to the message is through: context.PhysicalMessage
                    if (auditResult.DoNotAuditMessage)
                    {
                        return;
                    }
                    MessageAuditer.ForwardMessageToAuditQueue(context.PhysicalMessage);
                }

                public void Invoke(ReceiveLogicalMessageContext context, Action next)
                {
                    if (context.LogicalMessage.MessageType == typeof(MessageToBeAudited))
                    {
                        context.Get<AuditFilterResult>().DoNotAuditMessage = true;
                    }
                }

                class AuditFilterResult
                {
                    public bool DoNotAuditMessage { get; set; }
                }

                public override void Override(BehaviorList<ReceivePhysicalMessageContext> behaviorList)
                {
                    behaviorList.Replace<AuditBehavior, MyCustomAuditBehavior>();
                }

                public override void Override(BehaviorList<ReceiveLogicalMessageContext> behaviorList)
                {
                    behaviorList.Add<MyCustomAuditBehavior>();
                }
            }

#pragma warning restore 618
        }

      

        public class AuditSpy : EndpointConfigurationBuilder
        {

            public AuditSpy()
            {
                EndpointSetup<DefaultServer>();
            }

            class AuditMessageHandler : IHandleMessages<MessageToBeAudited>
            {
                public Context MyContext { get; set; }

                public void Handle(MessageToBeAudited message)
                {
                    MyContext.MessageAudited = true;
                }
            }
        }

        [Serializable]
        public class MessageToBeAudited : IMessage
        {
        }
    }

}
