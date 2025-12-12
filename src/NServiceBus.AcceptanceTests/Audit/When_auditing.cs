namespace NServiceBus.AcceptanceTests.Audit;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using EndpointTemplates;
using Features;
using NUnit.Framework;

public class When_auditing : NServiceBusAcceptanceTest
{
    const string AuditAddressEnvironmentVariableKey = "NServiceBus__Audit__Address";
    const string AuditIsEnabledEnvironmentVariableKey = "NServiceBus__Audit__IsEnabled";

    [Test]
    public async Task Should_not_be_forwarded_to_auditQueue_when_audit_feature_is_disabled_in_code()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithAuditFeatureDisabledInCode>(b => b.When(session => session.SendLocal(new MessageToBeAudited())))
            .WithEndpoint<EndpointThatHandlesAuditMessages>()
            .Run();

        Assert.That(context.IsMessageHandledByTheAuditEndpoint, Is.False);
    }

    [TestCase("false")]
    [TestCase("FALSE")]
    [TestCase("False")]
    [NonParallelizable]
    public async Task Should_not_be_forwarded_to_auditQueue_when_audit_feature_is_disabled_by_environment_variable(string auditEnabledValue)
    {
        var originalValue = Environment.GetEnvironmentVariable(AuditIsEnabledEnvironmentVariableKey);
        Environment.SetEnvironmentVariable(AuditIsEnabledEnvironmentVariableKey, auditEnabledValue);

        try
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithAuditQueueConfiguredInCode>(b => b.When(session => session.SendLocal(new MessageToBeAudited())))
                .WithEndpoint<EndpointThatHandlesAuditMessages>()
                .Done(c => c.IsMessageHandlingComplete)
                .Run();

            Assert.That(context.IsMessageHandledByTheAuditEndpoint, Is.False);
        }
        finally
        {
            Environment.SetEnvironmentVariable(AuditIsEnabledEnvironmentVariableKey, originalValue);
        }
    }


    [Test]
    public async Task Should_be_forwarded_to_auditQueue_when_auditing_is_configured_in_code()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithAuditQueueConfiguredInCode>(b => b.When(session => session.SendLocal(new MessageToBeAudited())))
            .WithEndpoint<EndpointThatHandlesAuditMessages>()
            .Done(c => c.IsMessageHandlingComplete && c.IsMessageHandledByTheAuditEndpoint)
            .Run();

        Assert.That(context.IsMessageHandledByTheAuditEndpoint, Is.True);
    }

    [Test]
    [NonParallelizable]
    public async Task Should_be_forwarded_to_auditQueue_when_auditing_is_configured_by_environment_variable()
    {
        var originalValue = Environment.GetEnvironmentVariable(AuditAddressEnvironmentVariableKey);

        var auditAddress = Conventions.EndpointNamingConvention(typeof(EndpointThatHandlesAuditMessages));
        Environment.SetEnvironmentVariable(AuditAddressEnvironmentVariableKey, auditAddress);

        try
        {
            var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithNoAuditQueueConfiguredInCode>(b => b.When(session => session.SendLocal(new MessageToBeAudited())))
            .WithEndpoint<EndpointThatHandlesAuditMessages>()
            .Run();

            Assert.That(context.IsMessageHandledByTheAuditEndpoint, Is.True);
        }
        finally
        {
            Environment.SetEnvironmentVariable(AuditAddressEnvironmentVariableKey, originalValue);
        }
    }

    public class Context : ScenarioContext
    {
        public bool IsMessageHandlingComplete { get; set; }
        public bool IsMessageHandledByTheAuditEndpoint { get; set; }

        public void MarkAsCompletedForAuditingDisabled() => MarkAsCompleted(IsMessageHandlingComplete);

        public void MarkAsCompletedForAuditingEnabled() => MarkAsCompleted(IsMessageHandlingComplete, IsMessageHandledByTheAuditEndpoint);
    }

    public class EndpointWithAuditFeatureDisabledInCode : EndpointConfigurationBuilder
    {
        public EndpointWithAuditFeatureDisabledInCode() =>
            // Although the AuditProcessedMessagesTo seems strange here, this test tries to fake the scenario where
            // even though the user has specified audit config, because auditing is explicitly turned
            // off, no messages should be audited.
            EndpointSetup<DefaultServer>(c =>
            {
                c.DisableFeature<Audit>();
                c.AuditProcessedMessagesTo<EndpointThatHandlesAuditMessages>();
            });

        class MessageToBeAuditedHandler(Context testContext) : IHandleMessages<MessageToBeAudited>
        {
            public Task Handle(MessageToBeAudited message, IMessageHandlerContext context)
            {
                testContext.IsMessageHandlingComplete = true;
                testContext.MarkAsCompletedForAuditingDisabled();
                return Task.CompletedTask;
            }
        }
    }

    public class EndpointWithAuditQueueConfiguredInCode : EndpointConfigurationBuilder
    {
        public EndpointWithAuditQueueConfiguredInCode() => EndpointSetup<DefaultServer>(c => c.AuditProcessedMessagesTo<EndpointThatHandlesAuditMessages>());

        class MessageToBeAuditedHandler(Context testContext) : IHandleMessages<MessageToBeAudited>
        {
            public Task Handle(MessageToBeAudited message, IMessageHandlerContext context)
            {
                testContext.IsMessageHandlingComplete = true;
                testContext.MarkAsCompletedForAuditingEnabled();
                return Task.CompletedTask;
            }
        }
    }

    public class EndpointWithNoAuditQueueConfiguredInCode : EndpointConfigurationBuilder
    {
        public EndpointWithNoAuditQueueConfiguredInCode() => EndpointSetup<DefaultServer>();

        class MessageToBeAuditedHandler : IHandleMessages<MessageToBeAudited>
        {
            public MessageToBeAuditedHandler(Context context)
            {
                testContext = context;
            }

            public Task Handle(MessageToBeAudited message, IMessageHandlerContext context)
            {
                testContext.IsMessageHandlingComplete = true;
                return Task.CompletedTask;
            }

            Context testContext;
        }
    }

    public class EndpointThatHandlesAuditMessages : EndpointConfigurationBuilder
    {
        public EndpointThatHandlesAuditMessages() => EndpointSetup<DefaultServer>();

        class AuditMessageHandler(Context testContext) : IHandleMessages<MessageToBeAudited>
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