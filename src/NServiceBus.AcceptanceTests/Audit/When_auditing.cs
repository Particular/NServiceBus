
namespace NServiceBus.AcceptanceTests.Audit
{
    using System;
    using System.Threading.Tasks;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;

    public class When_auditing : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_be_forwarded_to_auditQueue_when_auditing_is_disabled()
        {
            var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithAuditOff>(b => b.When(bus => bus.SendLocal(new MessageToBeAudited())))
            .WithEndpoint<EndpointThatHandlesAuditMessages>()
            .Done(c => c.IsMessageHandlingComplete)
            .Run();

            Assert.IsFalse(context.IsMessageHandledByTheAuditEndpoint);
        }

        [Test]
        public async Task Should_be_forwarded_to_auditQueue_when_auditing_is_enabled()
        {
            var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithAuditOn>(b => b.When(bus => bus.SendLocal(new MessageToBeAudited())))
            .WithEndpoint<EndpointThatHandlesAuditMessages>()
            .Done(c => c.IsMessageHandlingComplete && c.IsMessageHandledByTheAuditEndpoint)
            .Run();

            Assert.IsTrue(context.IsMessageHandledByTheAuditEndpoint);
        }

        public class Context : ScenarioContext
        {
            public bool IsMessageHandlingComplete { get; set; }
            public bool IsMessageHandledByTheAuditEndpoint { get; set; }
        }

        public class EndpointWithAuditOff : EndpointConfigurationBuilder
        {

            public EndpointWithAuditOff()
            {
                // Although the AuditTo seems strange here, this test tries to fake the scenario where
                // even though the user has specified audit config, because auditing is explicitly turned
                // off, no messages should be audited.
                EndpointSetup<DefaultServer>(c => c.DisableFeature<Features.Audit>())
                    .AuditTo<EndpointThatHandlesAuditMessages>();

            }

            class MessageToBeAuditedHandler : IHandleMessages<MessageToBeAudited>
            {
                public Context MyContext { get; set; }

                public Task Handle(MessageToBeAudited message, IMessageHandlerContext context)
                {
                    MyContext.IsMessageHandlingComplete = true;
                    return Task.FromResult(0);
                }
            }
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
                public Context MyContext { get; set; }

                public Task Handle(MessageToBeAudited message, IMessageHandlerContext context)
                {
                    MyContext.IsMessageHandlingComplete = true;
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
                public Context MyContext { get; set; }

                public Task Handle(MessageToBeAudited message, IMessageHandlerContext context)
                {
                    MyContext.IsMessageHandledByTheAuditEndpoint = true;
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
