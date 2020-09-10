namespace NServiceBus.AcceptanceTests.Audit
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_audit_is_overridden_in_code : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_audit_to_target_queue()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<UserEndpoint>(b => b.When(session => session.SendLocal(new MessageToBeAudited())))
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
                public Task Handle(MessageToBeAudited message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    return Task.FromResult(0);
                }
            }
        }

        public class AuditSpy : EndpointConfigurationBuilder
        {
            public AuditSpy()
            {
                EndpointSetup<DefaultServer>()
                    .CustomEndpointName("audit_with_code_target");
            }

            class AuditMessageHandler : IHandleMessages<MessageToBeAudited>
            {
                public AuditMessageHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(MessageToBeAudited message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    testContext.MessageAudited = true;
                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        public class Context : ScenarioContext
        {
            public bool MessageAudited { get; set; }
        }


        public class MessageToBeAudited : IMessage
        {
        }
    }
}