namespace NServiceBus.AcceptanceTests.Reliability.Outbox
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    public class When_a_message_is_audited : NServiceBusAcceptanceTest
    {

        [Test]
        public void Should_audit_even_if_dispatch_blows_once()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<EndpointWithAuditOn>(b => b.Given(bus => bus.SendLocal(new MessageToBeAudited())))
                    .WithEndpoint<AuditSpyEndpoint>()
                    .Done(c => c.Done)
                    .Run();
            Assert.True(context.Done);
        }



        public class Context : ScenarioContext
        {
            public bool Done { get; set; }
        }

        public class EndpointWithAuditOn : EndpointConfigurationBuilder
        {
            public EndpointWithAuditOn()
            {
                EndpointSetup<DefaultServer>(
                    b =>
                    {
                        b.GetSettings().Set("DisableOutboxTransportCheck", true);
                        b.EnableOutbox();
                        b.Pipeline.Register<BlowUpAfterDispatchBehavior.Registration>();
                    })
                    .AuditTo<AuditSpyEndpoint>();
            }

            public class BlowUpAfterDispatchBehavior : PhysicalMessageProcessingStageBehavior
            {
                public class Registration : RegisterStep
                {
                    public Registration()
                        : base("BlowUpAfterDispatchBehavior", typeof(BlowUpAfterDispatchBehavior), "For testing")
                    {
                        InsertAfter("FirstLevelRetries");
                        InsertBefore("OutboxDeduplication");
                    }
                }

                public override void Invoke(Context context, Action next)
                {
                    if (!context.GetIncomingPhysicalMessage().Headers[Headers.EnclosedMessageTypes].Contains(typeof(MessageToBeAudited).Name))
                    {
                        next();
                        return;
                    }


                    if (called)
                    {
                        Console.Out.WriteLine("Called once, skipping next");
                        return;

                    }
                    else
                    {
                        next();
                    }


                    called = true;

                    throw new Exception("Fake ex after dispatch");
                }

                static bool called;
            }


            public class MessageToBeAuditedHandler : IHandleMessages<MessageToBeAudited>
            {
                public void Handle(MessageToBeAudited message)
                {
                }
            }
        }

        class AuditSpyEndpoint : EndpointConfigurationBuilder
        {
            public AuditSpyEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MessageToBeAuditedHandler : IHandleMessages<MessageToBeAudited>
            {
                public Context Context { get; set; }
                public IBus Bus { get; set; }

                public void Handle(MessageToBeAudited message)
                {
                    Context.Done = true;
                }
            }
        }

        [Serializable]
        public class MessageToBeAudited : IMessage
        {
            public string RunId { get; set; }
        }
    }
}
