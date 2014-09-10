
namespace NServiceBus.AcceptanceTests.PipelineExtension
{
    using System;
    using System.Linq;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;
    using Pipeline;
    using Pipeline.Contexts;
    using Unicast.Behaviors;

    /// <summary>
    /// This is a demo on how pipeline overrides can be used to control which messages that gets audited by NServiceBus
    /// </summary>
    public class MutingHandlerExceptions : NServiceBusAcceptanceTest
    {
        [Test]
        public void RunDemo()
        {
            var context = new Context();
            Scenario.Define(context)
                .WithEndpoint<EndpointWithCustomExceptionMuting>(b => b.Given(bus => bus.SendLocal(new MessageThatWillBlowUpButExWillBeMuted())))
                .WithEndpoint<AuditSpy>()
                .Done(c => c.IsMessageHandlingComplete)
                .Run();

            Assert.IsTrue(context.MessageAudited);
        }


        public class EndpointWithCustomExceptionMuting : EndpointConfigurationBuilder
        {

            public EndpointWithCustomExceptionMuting()
            {
                EndpointSetup<DefaultServer>()
                    .AuditTo<AuditSpy>();

            }

            class Handler : IHandleMessages<MessageThatWillBlowUpButExWillBeMuted>
            {
                public Context MyContext { get; set; }

                public void Handle(MessageThatWillBlowUpButExWillBeMuted message)
                {
                    MyContext.IsMessageHandlingComplete = true;

                    throw new Exception("Lets filter on this text");
                }
            }

#pragma warning disable 618
            class MyExceptionFilteringBehavior : IBehavior<HandlerInvocationContext>
            {
                public void Invoke(HandlerInvocationContext context, Action next)
                {
                    try
                    {
                        //invoke the handler/rest of the pipeline
                        next();
                    }
                    catch (AggregateException ex)
                    {
                        //modify this to your liking
                        if (ex.InnerExceptions.First().Message == "Lets filter on this text")
                        {
                            return;
                        }
                        throw;
                    }
                    catch (Exception ex)
                    {
                        //modify this to your liking
                        if (ex.Message == "Lets filter on this text")
                        {
                            return;
                        }
                        throw;
                    }
                }


                //here we inject our behavior
                class MyExceptionFilteringOverride : PipelineOverride
                {
                    public override void Override(BehaviorList<HandlerInvocationContext> behaviorList)
                    {
                        //add our behavior to the pipeline just before NSB actually calls the handlers
                        behaviorList.InsertBefore<InvokeHandlersBehavior, MyExceptionFilteringBehavior>();
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

            class AuditMessageHandler : IHandleMessages<MessageThatWillBlowUpButExWillBeMuted>
            {
                public Context MyContext { get; set; }

                public void Handle(MessageThatWillBlowUpButExWillBeMuted message)
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
        public class MessageThatWillBlowUpButExWillBeMuted : IMessage
        {
        }
    }
}
