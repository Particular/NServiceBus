namespace NServiceBus.AcceptanceTests.Core.Diagnostics
{
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
                .Done(c => c.Done)
                .Run();

            Assert.IsNotNull(context.HostId);
            Assert.IsNotNull(context.HostName);
            Assert.IsNotNull(context.Endpoint);
            Assert.IsNotNull(context.Machine);
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
                EndpointSetup<DefaultServer>(c => c
                    .AuditProcessedMessagesTo<AuditSpyEndpoint>());
            }

            public class MessageToBeAuditedHandler : IHandleMessages<MessageToBeAudited>
            {
                public Task Handle(MessageToBeAudited message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    return Task.FromResult(0);
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
                public MessageToBeAuditedHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(MessageToBeAudited message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    testContext.HostId = context.MessageHeaders[Headers.HostId];
                    testContext.HostName = context.MessageHeaders[Headers.HostDisplayName];
                    testContext.Endpoint = context.MessageHeaders[Headers.ProcessingEndpoint];
                    testContext.Machine = context.MessageHeaders[Headers.ProcessingMachine];
                    testContext.Done = true;
                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }


        public class MessageToBeAudited : IMessage
        {
        }
    }
}