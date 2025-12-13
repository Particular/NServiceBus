namespace NServiceBus.AcceptanceTests.Core.Diagnostics;

using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using EndpointTemplates;
using NUnit.Framework;

public class When_a_message_is_audited : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_add_host_related_headers()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithAuditOn>(b => b.When((session, c) => session.SendLocal(new MessageToBeAudited())))
            .WithEndpoint<AuditSpyEndpoint>()
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.HostId, Is.Not.Null);
            Assert.That(context.HostName, Is.Not.Null);
            Assert.That(context.Endpoint, Is.Not.Null);
            Assert.That(context.Machine, Is.Not.Null);
        }
    }

    public class Context : ScenarioContext
    {
        public string HostId { get; set; }
        public string HostName { get; set; }
        public string Endpoint { get; set; }
        public string Machine { get; set; }
    }

    public class EndpointWithAuditOn : EndpointConfigurationBuilder
    {
        public EndpointWithAuditOn() =>
            EndpointSetup<DefaultServer>(c => c
                .AuditProcessedMessagesTo<AuditSpyEndpoint>());

        public class MessageToBeAuditedHandler : IHandleMessages<MessageToBeAudited>
        {
            public Task Handle(MessageToBeAudited message, IMessageHandlerContext context) => Task.CompletedTask;
        }
    }

    class AuditSpyEndpoint : EndpointConfigurationBuilder
    {
        public AuditSpyEndpoint() => EndpointSetup<DefaultServer>();

        public class MessageToBeAuditedHandler(Context testContext) : IHandleMessages<MessageToBeAudited>
        {
            public Task Handle(MessageToBeAudited message, IMessageHandlerContext context)
            {
                testContext.HostId = context.MessageHeaders[Headers.HostId];
                testContext.HostName = context.MessageHeaders[Headers.HostDisplayName];
                testContext.Endpoint = context.MessageHeaders[Headers.ProcessingEndpoint];
                testContext.Machine = context.MessageHeaders[Headers.ProcessingMachine];
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class MessageToBeAudited : IMessage;
}