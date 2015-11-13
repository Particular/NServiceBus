namespace NServiceBus.AcceptanceTests.Audit
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_audit_is_overridden_in_code : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_audit_to_target_queue()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<UserEndpoint>(b => b.When(bus => bus.SendLocal(new MessageToBeAudited())))
                .WithEndpoint<AuditSpy>()
                .Done(c => c.MessageAudited)
                .Run();

            Assert.True(context.MessageAudited);
        }

        public class UserEndpoint : EndpointConfigurationBuilder
        {
            public UserEndpoint()
            {
                EndpointSetup<DefaultServer>(c => c.AuditProcessedMessagesTo("audit_with_code_target"));
            }

            class Handler : IHandleMessages<MessageToBeAudited>
            {
                public Task Handle(MessageToBeAudited message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
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

                public Task Handle(MessageToBeAudited message, IMessageHandlerContext context)
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
        public class MessageToBeAudited : IMessage
        {
        }

    }
}
