namespace NServiceBus.AcceptanceTests.Audit
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_audit_is_everriden_in_code : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_audit_to_target_queue()
        {
            var context = new Context();
            Scenario.Define(context)
                .WithEndpoint<UserEndpoint>(b => b.Given(bus => bus.SendLocal(new MessageToBeAudited())))
                .WithEndpoint<AuditSpy>()
                .Done(c => c.MessageAudited)
                .Run();
        }

        public class UserEndpoint : EndpointConfigurationBuilder
        {
            public UserEndpoint()
            {
                EndpointSetup<DefaultServer>(c => c.AuditProcessedMessagesTo("audit_with_code_source"));
            }

            class Handler : IHandleMessages<MessageToBeAudited>
            {
                public IBus Bus { get; set; }

                public void Handle(MessageToBeAudited message)
                {
                }
            }

        }

        public class AuditSpy : EndpointConfigurationBuilder
        {
            public AuditSpy()
            {
                EndpointSetup<DefaultServer>(c => c.EndpointName("audit_with_code_target"));
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
            public bool MessageAudited { get; set; }
        }


        [Serializable]
        public class MessageToBeAudited : IMessage
        {
        }

    }
}
