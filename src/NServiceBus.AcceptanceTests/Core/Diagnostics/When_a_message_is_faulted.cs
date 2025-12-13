namespace NServiceBus.AcceptanceTests.Core.Diagnostics;

using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using EndpointTemplates;
using NUnit.Framework;

public class When_a_message_is_faulted : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_add_host_related_headers()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithFailingMessage>(b => b.When((session, c) => session.SendLocal(new MessageThatFails())).DoNotFailOnErrorMessages())
            .WithEndpoint<EndpointThatHandlesErrorMessages>()
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.HostId, Is.Not.Null, "Host Id should be included in fault message headers");
            Assert.That(context.HostName, Is.Not.Null, "Host Name should be included in fault message headers");
            Assert.That(context.Endpoint, Is.Not.Null, "Endpoint name should be included in fault message headers.");
            Assert.That(context.Machine, Is.Not.Null, "Machine should be included in fault message headers.");
        }
    }

    public class Context : ScenarioContext
    {
        public string HostId { get; set; }
        public string HostName { get; set; }
        public string Endpoint { get; set; }
        public string Machine { get; set; }
    }

    public class EndpointWithFailingMessage : EndpointConfigurationBuilder
    {
        public EndpointWithFailingMessage() =>
            EndpointSetup<DefaultServer>(c =>
            {
                c.SendFailedMessagesTo<EndpointThatHandlesErrorMessages>();
            });

        public class MessageToBeAuditedHandler : IHandleMessages<MessageThatFails>
        {
            public Task Handle(MessageThatFails message, IMessageHandlerContext context) => throw new SimulatedException();
        }
    }

    class EndpointThatHandlesErrorMessages : EndpointConfigurationBuilder
    {
        public EndpointThatHandlesErrorMessages() => EndpointSetup<DefaultServer>();

        public class MessageThatFailsHandler(Context testContext) : IHandleMessages<MessageThatFails>
        {
            public Task Handle(MessageThatFails message, IMessageHandlerContext context)
            {
                testContext.HostId = context.MessageHeaders.ContainsKey(Headers.HostId) ? context.MessageHeaders[Headers.HostId] : null;
                testContext.HostName = context.MessageHeaders.ContainsKey(Headers.HostDisplayName) ? context.MessageHeaders[Headers.HostDisplayName] : null;
                testContext.Endpoint = context.MessageHeaders.ContainsKey(Headers.ProcessingEndpoint) ? context.MessageHeaders[Headers.ProcessingEndpoint] : null;
                testContext.Machine = context.MessageHeaders.ContainsKey(Headers.ProcessingMachine) ? context.MessageHeaders[Headers.ProcessingMachine] : null;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class MessageThatFails : IMessage;
}