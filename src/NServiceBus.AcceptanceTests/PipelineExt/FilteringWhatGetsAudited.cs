﻿
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
                .Done(c => c.Done)
                .Run();

            Assert.IsFalse(context.WrongMessageAudited);
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
                public IBus Bus { get; set; }

                public void Handle(MessageToBeAudited message)
                {
                    Bus.SendLocal(new Message3());
                }
            }

            class Message3Handler : IHandleMessages<Message3>
            {
                public void Handle(Message3 message)
                {
                }
            }

            class SetFiltering : LogicalMessageProcessingStageBehavior
            {
                public override void Invoke(Context context, Action next)
                {
                    if (context.IncomingLogicalMessage.MessageType == typeof(MessageToBeAudited))
                    {
                        context.Get<AuditFilterResult>().DoNotAuditMessage = true;
                    }

                    next();
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

            class FilteringAuditBehavior : PhysicalMessageProcessingStageBehavior
            {
                public IAuditMessages MessageAuditer { get; set; }

                public override void Invoke(Context context, Action next)
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
                    MessageAuditer.Audit(new SendOptions("auditspy"), new OutgoingMessage(context.PhysicalMessage.Id, context.PhysicalMessage.Headers, context.PhysicalMessage.Body));
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
                EndpointSetup<DefaultServer>(c => c.EndpointName("auditspy"));
            }

            class AuditMessageHandler : IHandleMessages<MessageToBeAudited>
            {
                public Context MyContext { get; set; }

                public void Handle(MessageToBeAudited message)
                {
                    MyContext.WrongMessageAudited = true;
                }
            }

            class Message3Handler : IHandleMessages<Message3>
            {
                public Context MyContext { get; set; }

                public void Handle(Message3 message)
                {
                    MyContext.Done = true;
                }
            }
        }

        public class Context : ScenarioContext
        {
            public bool Done { get; set; }
            public bool WrongMessageAudited { get; set; }
        }


        [Serializable]
        public class MessageToBeAudited : IMessage
        {
        }

        [Serializable]
        public class Message3 : IMessage
        {
        }
    }
}
