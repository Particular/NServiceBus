namespace NServiceBus.AcceptanceTests.Audit;

using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using EndpointTemplates;
using Features;
using NUnit.Framework;

public class When_auditing : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_not_be_forwarded_to_auditQueue_when_auditing_is_disabled()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithAuditOff>(b => b.When(session => session.SendLocal(new MessageToBeAudited())))
            .WithEndpoint<EndpointThatHandlesAuditMessages>()
            .Run();

        Assert.That(context.IsMessageHandledByTheAuditEndpoint, Is.False);
    }

    [Test]
    public async Task Should_be_forwarded_to_auditQueue_when_auditing_is_enabled()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithAuditOn>(b => b.When(session => session.SendLocal(new MessageToBeAudited())))
            .WithEndpoint<EndpointThatHandlesAuditMessages>()
            .Run();

        Assert.That(context.IsMessageHandledByTheAuditEndpoint, Is.True);
    }

    public class Context : ScenarioContext
    {
        public bool IsMessageHandlingComplete { get; set; }
        public bool IsMessageHandledByTheAuditEndpoint { get; set; }

        public void MarkAsCompletedForAuditingDisabled() => MarkAsCompleted(IsMessageHandlingComplete);

        public void MarkAsCompletedForAuditingEnabled() => MarkAsCompleted(IsMessageHandlingComplete, IsMessageHandledByTheAuditEndpoint);
    }

    public class EndpointWithAuditOff : EndpointConfigurationBuilder
    {
        public EndpointWithAuditOff() =>
            // Although the AuditProcessedMessagesTo seems strange here, this test tries to fake the scenario where
            // even though the user has specified audit config, because auditing is explicitly turned
            // off, no messages should be audited.
            EndpointSetup<DefaultServer>(c =>
            {
                c.DisableFeature<Audit>();
                c.AuditProcessedMessagesTo<EndpointThatHandlesAuditMessages>();
            });

        [Handler]
        public class MessageToBeAuditedHandler(Context testContext) : IHandleMessages<MessageToBeAudited>
        {
            public Task Handle(MessageToBeAudited message, IMessageHandlerContext context)
            {
                testContext.IsMessageHandlingComplete = true;
                testContext.MarkAsCompletedForAuditingDisabled();
                return Task.CompletedTask;
            }
        }
    }

    public class EndpointWithAuditOn : EndpointConfigurationBuilder
    {
        public EndpointWithAuditOn() => EndpointSetup<DefaultServer>(c => c.AuditProcessedMessagesTo<EndpointThatHandlesAuditMessages>());

        [Handler]
        public class MessageToBeAuditedHandler(Context testContext) : IHandleMessages<MessageToBeAudited>
        {
            public Task Handle(MessageToBeAudited message, IMessageHandlerContext context)
            {
                testContext.IsMessageHandlingComplete = true;
                testContext.MarkAsCompletedForAuditingEnabled();
                return Task.CompletedTask;
            }
        }
    }

    public class EndpointThatHandlesAuditMessages : EndpointConfigurationBuilder
    {
        public EndpointThatHandlesAuditMessages() => EndpointSetup<DefaultServer>();

        [Handler]
        public class AuditMessageHandler(Context testContext) : IHandleMessages<MessageToBeAudited>
        {
            public Task Handle(MessageToBeAudited message, IMessageHandlerContext context)
            {
                testContext.IsMessageHandledByTheAuditEndpoint = true;
                testContext.MarkAsCompletedForAuditingEnabled();
                return Task.CompletedTask;
            }
        }
    }

    public class MessageToBeAudited : IMessage;
}