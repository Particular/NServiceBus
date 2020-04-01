namespace NServiceBus.AcceptanceTests.Core.Diagnostics
{
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
                .WithEndpoint<EndpointWithAuditOn>(b => b.When((session, c) => session.SendLocal(new MessageThatFails())).DoNotFailOnErrorMessages())
                .WithEndpoint<EndpointThatHandlesErrorMessages>()
                .Done(c => c.Done)
                .Run();

            Assert.IsNotNull(context.HostId, "Host Id should be included in fault message headers");
            Assert.IsNotNull(context.HostName, "Host Name should be included in fault message headers");
            Assert.IsNotNull(context.Endpoint, "Endpoint name should be included in fault message headers.");
            Assert.IsNotNull(context.Machine, "Machine should be included in fault message headers.");
        }

        public class Context : ScenarioContext
        {
            public bool Done { get; set; }
            public string HostId { get; set; }
            public string HostName { get; set; }
            public string Endpoint { get; set; }
            public string Machine { get; set; }
        }

        public class EndpointWithAuditOn : EndpointConfigurationBuilder
        {
            public EndpointWithAuditOn()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.SendFailedMessagesTo<EndpointThatHandlesErrorMessages>();
                });
            }

            public class MessageToBeAuditedHandler : IHandleMessages<MessageThatFails>
            {
                public Task Handle(MessageThatFails message, IMessageHandlerContext context)
                {
                    throw new SimulatedException();
                }
            }
        }

        class EndpointThatHandlesErrorMessages : EndpointConfigurationBuilder
        {
            public EndpointThatHandlesErrorMessages()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MessageToBeAuditedHandler : IHandleMessages<MessageThatFails>
            {
                public MessageToBeAuditedHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(MessageThatFails message, IMessageHandlerContext context)
                {
                    testContext.HostId = context.MessageHeaders.ContainsKey(Headers.HostId) ? context.MessageHeaders[Headers.HostId] : null;
                    testContext.HostName = context.MessageHeaders.ContainsKey(Headers.HostDisplayName) ? context.MessageHeaders[Headers.HostDisplayName] : null;
                    testContext.Endpoint = context.MessageHeaders.ContainsKey(Headers.ProcessingEndpoint) ? context.MessageHeaders[Headers.ProcessingEndpoint] : null;
                    testContext.Machine = context.MessageHeaders.ContainsKey(Headers.ProcessingMachine) ? context.MessageHeaders[Headers.ProcessingMachine] : null;
                    testContext.Done = true;
                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        public class MessageThatFails : IMessage
        {
        }
    }
}