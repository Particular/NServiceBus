
namespace NServiceBus.AcceptanceTests.PipelineExt
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    /// <summary>
    /// This is a demo on how pipeline overrides can be used to control which messages that gets audited by NServiceBus
    /// </summary>
    public class MutingHandlerExceptions : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task RunDemo()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithCustomExceptionMuting>(b => b.When(bus => bus.SendLocal(new MessageThatWillBlowUpButExWillBeMuted())))
                .WithEndpoint<AuditSpy>()
                .Done(c => c.MessageAudited)
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

                public Task Handle(MessageThatWillBlowUpButExWillBeMuted message, IMessageHandlerContext context)
                {
                    throw new Exception("Lets filter on this text");
                }
            }

            class MyExceptionFilteringBehavior : Behavior<IIncomingPhysicalMessageContext>
            {
                public override async Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
                {
                    try
                    {
                        //invoke the handler/rest of the pipeline
                        await next().ConfigureAwait(false);
                    }
                    //catch specifix exceptions or
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
                class MyExceptionFilteringOverride : INeedInitialization
                {
                    public void Customize(BusConfiguration configuration)
                    {
                        configuration.Pipeline.Register<MyExceptionFilteringRegistration>();
                    }
                }

                class MyExceptionFilteringRegistration : RegisterStep
                {
                    public MyExceptionFilteringRegistration() : base("ExceptionFiltering", typeof(MyExceptionFilteringBehavior), "Custom exception filtering")
                    {
                        InsertAfter(WellKnownStep.AuditProcessedMessage);
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

                public Task Handle(MessageThatWillBlowUpButExWillBeMuted message, IMessageHandlerContext context)
                {
                    MyContext.MessageAudited = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class Context : ScenarioContext
        {
            public bool MessageAudited { get; set; }
        }

        [Serializable]
        public class MessageThatWillBlowUpButExWillBeMuted : IMessage
        {
        }
    }
}
