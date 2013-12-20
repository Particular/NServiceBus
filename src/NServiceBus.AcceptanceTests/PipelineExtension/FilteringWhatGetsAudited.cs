
namespace NServiceBus.AcceptanceTests.Audit
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Audit;
    using NUnit.Framework;
    using Pipeline;
    using Pipeline.Contexts;

    /// <summary>
    /// This is a demo on how pipeline overrides can be used to control which messages that gets audited by NServiceBus
    /// </summary>
    public class FilteringWhatGetsAudited : NServiceBusAcceptanceTest
    {
        [Test]
        public void RunDemo()
        {
            var context = new Context();
            Scenario.Define(context)
                .WithEndpoint<UserEndpoint>(b => b.Given(bus => bus.SendLocal(new MessageToBeAudited())))
                .WithEndpoint<AuditSpy>()
                .Done(c => c.IsMessageHandlingComplete)
                .Run();

            Assert.IsFalse(context.MessageAudited);
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
            class MyFilteringAuditBehavior : IBehavior<ReceivePhysicalMessageContext>,
                IBehavior<ReceiveLogicalMessageContext>
            {
                public MessageAuditer MessageAuditer { get; set; }

                public void Invoke(ReceivePhysicalMessageContext context, Action next)
                {
                    var auditResult = new AuditFilterResult();
                    context.Set(auditResult);
                    next();

                    //note: and rule operating on the raw TransportMessage can be applied here if needed.
                    // Access to the message is through: context.PhysicalMessage. Eg:  context.PhysicalMessage.Headers.ContainsKey("NServiceBus.ControlMessage")
                    if (auditResult.DoNotAuditMessage)
                    {
                        return;
                    }
                    MessageAuditer.ForwardMessageToAuditQueue(context.PhysicalMessage);
                }

                public void Invoke(ReceiveLogicalMessageContext context, Action next)
                {
                    //filter out messages of type MessageToBeAudited
                    if (context.LogicalMessage.MessageType == typeof(MessageToBeAudited))
                    {
                        context.Get<AuditFilterResult>().DoNotAuditMessage = true;
                    }
                }

                class AuditFilterResult
                {
                    public bool DoNotAuditMessage { get; set; }
                }


                //here we inject our behavior
                class AuditFilteringOverride : PipelineOverride
                {
                    public override void Override(BehaviorList<ReceivePhysicalMessageContext> behaviorList)
                    {
                        //we replace the default audit behavior with out own
                        behaviorList.Replace<AuditBehavior, MyFilteringAuditBehavior>();
                    }

                    public override void Override(BehaviorList<ReceiveLogicalMessageContext> behaviorList)
                    {
                        //and also hook into to logical receive pipeline to make filtering on message types easier
                        behaviorList.Add<MyFilteringAuditBehavior>();
                    }
                }

#pragma warning restore 618
            }
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

        public class Context : ScenarioContext
        {
            public bool IsMessageHandlingComplete { get; set; }
            public bool MessageAudited { get; set; }
        }


        [Serializable]
        public class MessageToBeAudited : IMessage
        {
        }
    }
}
