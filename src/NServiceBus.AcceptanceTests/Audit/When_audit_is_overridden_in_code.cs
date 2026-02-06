namespace NServiceBus.AcceptanceTests.Audit;

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
            .Run();

        Assert.That(context.MessageAudited, Is.True);
    }

    public class UserEndpoint : EndpointConfigurationBuilder
    {
        public UserEndpoint() => EndpointSetup<DefaultServer>(c => c.AuditProcessedMessagesTo("audit_with_code_target"));

        [Handler]
        public class Handler : IHandleMessages<MessageToBeAudited>
        {
            public Task Handle(MessageToBeAudited message, IMessageHandlerContext context) => Task.CompletedTask;
        }
    }

    public class AuditSpy : EndpointConfigurationBuilder
    {
        public AuditSpy() =>
            EndpointSetup<DefaultServer>()
                .CustomEndpointName("audit_with_code_target");

        [Handler]
        public class AuditMessageHandler(Context testContext) : IHandleMessages<MessageToBeAudited>
        {
            public Task Handle(MessageToBeAudited message, IMessageHandlerContext context)
            {
                testContext.MessageAudited = true;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class Context : ScenarioContext
    {
        public bool MessageAudited { get; set; }
    }


    public class MessageToBeAudited : IMessage;
}