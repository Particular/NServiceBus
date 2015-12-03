namespace NServiceBus.AcceptanceTests.Performance
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_message_is_audited : NServiceBusAcceptanceTest
    {

        [Test]
        public async Task Should_contain_processing_stats_headers()
        {
            var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithAuditOn>(b => b.When(bus => bus.SendLocal(new MessageToBeAudited())))
            .WithEndpoint<EndpointThatHandlesAuditMessages>()
            .Done(c => c.IsMessageHandledByTheAuditEndpoint)
            .Run();

            Assert.IsTrue(context.Headers.ContainsKey(Headers.ProcessingStarted));
            Assert.IsTrue(context.Headers.ContainsKey(Headers.ProcessingEnded));
            Assert.IsTrue(context.IsMessageHandledByTheAuditEndpoint);
        }

        public class Context : ScenarioContext
        {
            public bool IsMessageHandledByTheAuditEndpoint { get; set; }
            public IReadOnlyDictionary<string, string> Headers { get; set; }
        }

        public class EndpointWithAuditOn : EndpointConfigurationBuilder
        {

            public EndpointWithAuditOn()
            {
                EndpointSetup<DefaultServer>()
                    .AuditTo<EndpointThatHandlesAuditMessages>();
            }

            class MessageToBeAuditedHandler : IHandleMessages<MessageToBeAudited>
            {
                public Task Handle(MessageToBeAudited message, IMessageHandlerContext context1)
                {
                    return Task.FromResult(0);
                }
            }
        }

        public class EndpointThatHandlesAuditMessages : EndpointConfigurationBuilder
        {

            public EndpointThatHandlesAuditMessages()
            {
                EndpointSetup<DefaultServer>();
            }

            class AuditMessageHandler : IHandleMessages<MessageToBeAudited>
            {
                Context testContext;

                public AuditMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(MessageToBeAudited message, IMessageHandlerContext context)
                {
                    testContext.Headers = context.MessageHeaders;
                    testContext.IsMessageHandledByTheAuditEndpoint = true;
                    return Task.FromResult(0);
                }
            }
        }

        [Serializable]
        public class MessageToBeAudited : IMessage
        {
        }
    }
}
