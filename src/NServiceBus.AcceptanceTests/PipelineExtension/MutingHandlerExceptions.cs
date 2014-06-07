﻿
namespace NServiceBus.AcceptanceTests.PipelineExtension
{
    using System;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;
    using Pipeline;
    using Pipeline.Contexts;

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

            class MyExceptionFilteringBehavior : IBehavior<IncomingContext>
            {
                public void Invoke(IncomingContext context, Action next)
                {
                    try
                    {
                        //invoke the handler/rest of the pipeline
                        next();
                    }
                    //catch specifix exceptions or
                    catch (Exception ex)
                    {
                        //modify this to your liking
                        if (ex.Message == "Lets filter on this text")
                            return;

                        throw;
                    }
                }

                //here we inject our behavior
                class MyExceptionFilteringOverride : INeedInitialization
                {
                    public void Init(Configure config)
                    {
                        //add our behavior to the pipeline just before NSB actually calls the handlers

                        config.Pipeline.Register<MyExceptionFilteringRegistration>();
                    }
                }

                class MyExceptionFilteringRegistration : RegisterBehavior
                {
                    public MyExceptionFilteringRegistration()
                        : base("ExceptionFiltering", typeof(MyExceptionFilteringBehavior), "Custom exception filtering")
                    {
                        InsertAfter(WellKnownBehavior.AuditProcessedMessage);
                        InsertBefore(WellKnownBehavior.InvokeHandlers);
                    }
                }
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
