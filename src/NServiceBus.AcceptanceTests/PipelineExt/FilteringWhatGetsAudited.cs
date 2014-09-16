
namespace NServiceBus.AcceptanceTests.PipelineExt
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Transports;
    using NServiceBus.Unicast;
    using NUnit.Framework;

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

            class SetFiltering : IBehavior<IncomingContext>
            {
                public void Invoke(IncomingContext context, Action next)
                {
                    if (context.IncomingLogicalMessage.MessageType == typeof(MessageToBeAudited))
                    {
                        context.Get<AuditFilterResult>().DoNotAuditMessage = true;
                    }
                }

                class AuditFilteringOverride : INeedInitialization
                {
                    public void Customize(BusConfiguration configuration)
                    {
                        configuration.Pipeline.Register("SetFiltering", typeof(SetFiltering), "Filters audit entries");
                    }
                }
            }

            class AuditFilterResult
            {
                public bool DoNotAuditMessage { get; set; }
            }

            class FilteringAuditBehavior : IBehavior<IncomingContext>
            {
                public IAuditMessages MessageAuditer { get; set; }

                public void Invoke(IncomingContext context, Action next)
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
                    MessageAuditer.Audit(new SendOptions("audit"),context.PhysicalMessage);
                }

                //here we inject our behavior
                class AuditFilteringOverride : INeedInitialization
                {
                    public void Customize(BusConfiguration configuration)
                    {
                        //we replace the default audit behavior with out own
                        configuration.Pipeline.Replace(WellKnownStep.AuditProcessedMessage, typeof(FilteringAuditBehavior), "A new audit forwarder that has filtering");
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
