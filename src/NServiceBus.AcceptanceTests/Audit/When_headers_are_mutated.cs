namespace NServiceBus.AcceptanceTests.Audit;

using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using EndpointTemplates;
using MessageMutator;
using NUnit.Framework;

public class When_headers_are_mutated : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_audit_original_headers()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<MutatingEndpoint>(b => b.When(session =>
            {
                var options = new SendOptions();
                options.RouteToThisEndpoint();
                options.SetHeader("AuditTestHeader", "original");
                return session.Send(new MessageToBeAudited(), options);
            }))
            .WithEndpoint<AuditSpyEndpoint>()
            .Run();

        Assert.That(context.AuditedHeader, Is.EqualTo("original"));
    }

    public class Context : ScenarioContext
    {
        public string AuditedHeader { get; set; }
    }

    public class MutatingEndpoint : EndpointConfigurationBuilder
    {
        public MutatingEndpoint() => EndpointSetup<DefaultServer>(c =>
        {
            c.AuditProcessedMessagesTo<AuditSpyEndpoint>();
            c.RegisterMessageMutator(new HeaderMutator());
        });

        class HeaderMutator : IMutateIncomingTransportMessages
        {
            public Task MutateIncoming(MutateIncomingTransportMessageContext context)
            {
                context.Headers["AuditTestHeader"] = "mutated";
                return Task.CompletedTask;
            }
        }

        [Handler]
        public class MessageHandler : IHandleMessages<MessageToBeAudited>
        {
            public Task Handle(MessageToBeAudited message, IMessageHandlerContext context) => Task.CompletedTask;
        }
    }

    public class AuditSpyEndpoint : EndpointConfigurationBuilder
    {
        public AuditSpyEndpoint() => EndpointSetup<DefaultServer>();

        [Handler]
        public class MessageHandler(Context testContext) : IHandleMessages<MessageToBeAudited>
        {
            public Task Handle(MessageToBeAudited message, IMessageHandlerContext context)
            {
                testContext.AuditedHeader = context.MessageHeaders["AuditTestHeader"];
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class MessageToBeAudited : IMessage;
}
