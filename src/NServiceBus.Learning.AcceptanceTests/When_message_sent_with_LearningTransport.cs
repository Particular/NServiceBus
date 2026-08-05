namespace NServiceBus.AcceptanceTests;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using EndpointTemplates;
using NServiceBus.Pipeline;
using NUnit.Framework;
using Transport;

public class When_message_sent_with_LearningTransport : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_preserve_file_created_time_as_receive_property()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(b => b.When(session => session.SendLocal(new TestMessage())))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.MessageReceived, Is.True, "Message was not received");
            Assert.That(context.ReceiveFileCreatedAt, Is.Not.Null, "FileCreatedAt property should be present");
            Assert.That(DateTime.TryParse(context.ReceiveFileCreatedAt, out _), Is.True, "FileCreatedAt should be a valid datetime");
        }
    }

    [Test]
    public async Task Should_preserve_file_created_time_property_on_dispatched_copies()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithAuditOn>(b => b.When(session => session.SendLocal(new TestMessage())))
            .WithEndpoint<AuditSpy>()
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.MessageAudited, Is.True, "Message was not audited");
            Assert.That(context.ReceiveFileCreatedAt, Is.Not.Null, "FileCreatedAt property should be present");
            Assert.That(DateTime.TryParse(context.ReceiveFileCreatedAt, out _), Is.True, "FileCreatedAt should be a valid datetime");
        }
    }

    [Test]
    public async Task Should_preserve_file_created_time_property_on_delayed_retry()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<FailingEndpoint>(b => b
                .When(session => session.SendLocal(new TestMessage()))
                .DoNotFailOnErrorMessages())
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.NumberOfRetries, Is.EqualTo(2), "Message was not retried");
            Assert.That(context.ReceiveFileCreatedAt, Is.Not.Null, "FileCreatedAt property should be present");
            Assert.That(DateTime.TryParse(context.ReceiveFileCreatedAt, out _), Is.True, "FileCreatedAt should be a valid datetime");
        }
    }

    [Test]
    public async Task Should_not_preserve_receive_properties_on_outgoing_messages()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<SendingEndpoint>(b => b.When(session => session.SendLocal(new TestMessage())))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.MessageReceived, Is.True, "Message was received");
            Assert.That(DateTime.TryParse(context.ReceiveFileCreatedAt, out _), Is.True, "FileCreatedAt should be a valid datetime");
            Assert.That(DateTime.TryParse(context.SendFileCreatedAt, out _), Is.True, "SendFileCreatedAt should be a valid datetime");
        }
    }

    [Test]
    public async Task Should_not_override_audit_properties_with_receive_properties_when_dispatch_properties_are_used()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndPointThatReceivesFromAnotherAndAuditsEndpoint>(b => b.When(session => session.SendLocal(new OutgoingTestMessage())))
            .WithEndpoint<AuditSpyForEndPointThatReceivesFromAnotherAndAuditsEndpoint>()
            .Run();

        Assert.That(context.MessageReceived, Is.True, "Message was not received");
    }

    [Test]
    public async Task Should_preserve_file_created_time_property_when_moved_to_error_queue()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithFailingHandler>(b => b
                .DoNotFailOnErrorMessages()
                .When((session, ctx) => session.SendLocal(new TestMessage()))
            )
            .WithEndpoint<ErrorSpy>()
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.MessageMovedToErrorQueue, Is.True, "Message was not moved to error queue");
            Assert.That(context.ReceiveFileCreatedAt, Is.Not.Null, "FileCreatedAt property should be present on message in error queue");
            Assert.That(DateTime.TryParse(context.ReceiveFileCreatedAt, out _), Is.True, "FileCreatedAt should be a valid datetime");
        }
    }

    [Test]
    public async Task Should_not_override_error_queue_dispatch_properties_with_receive_properties()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithFailingHandlerAndDispatchOverride>(b => b
                .DoNotFailOnErrorMessages()
                .When((session, ctx) => session.SendLocal(new TestMessage()))
            )
            .WithEndpoint<ErrorSpyWithDispatchPropertyVerification>()
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.MessageMovedToErrorQueue, Is.True, "Message was not moved to error queue");
            Assert.That(context.ReceiveFileCreatedAt, Is.Not.Null, "FileCreatedAt property should be captured from original message");
            Assert.That(context.ErrorQueueFileCreatedAtDiffersFromOriginal, Is.True, "Error queue message should have FileCreatedAt from dispatch properties, not receive properties");
        }
    }

    class Context : ScenarioContext
    {
        public bool MessageReceived { get; set; }
        public bool MessageAudited { get; set; }
        public bool MessageMovedToErrorQueue { get; set; }
        public bool ErrorQueueFileCreatedAtDiffersFromOriginal { get; set; }
        public int NumberOfRetries { get; set; }
        public string ReceiveFileCreatedAt { get; set; }
        public string RetryFileCreatedAt { get; set; }
        public string SendFileCreatedAt { get; set; }
    }

    class EndPointThatReceivesFromAnotherAndAuditsEndpoint : EndpointConfigurationBuilder
    {
        public EndPointThatReceivesFromAnotherAndAuditsEndpoint() => EndpointSetup<DefaultServer>(endpointConfiguration =>
        {
            endpointConfiguration.AuditProcessedMessagesTo(Conventions.EndpointNamingConvention(typeof(AuditSpyForEndPointThatReceivesFromAnotherAndAuditsEndpoint)));
            endpointConfiguration.Pipeline.Register(behavior: new AuditHeaderOverrideBehavior(), description: "Override headers on audit messages");
        });

        class OutgoingTestMessageHandler(Context testContext) : IHandleMessages<OutgoingTestMessage>
        {
            public Task Handle(OutgoingTestMessage message, IMessageHandlerContext context)
            {
                testContext.MessageReceived = true;

                if (context.Extensions.TryGet<IncomingMessage>(out var incomingMessage) && incomingMessage.ReceiveProperties.TryGetValue("LearningTransport.FileCreatedAt", out var fileCreatedAt))
                {
                    testContext.ReceiveFileCreatedAt = fileCreatedAt;
                }
                else
                {
                    testContext.MarkAsFailed(new Exception("Failed to propagate receive properties from the original message."));
                }

                return Task.CompletedTask;
            }
        }
    }

    class AuditHeaderOverrideBehavior : Behavior<IRoutingContext>
    {
        public override Task Invoke(IRoutingContext context, Func<Task> next)
        {
            context.Extensions.Get<DispatchProperties>()["LearningTransport.FileCreatedAt"] = DateTime.UtcNow.AddDays(10).ToString("o");

            return next();
        }
    }

    class AuditSpyForEndPointThatReceivesFromAnotherAndAuditsEndpoint : EndpointConfigurationBuilder
    {
        public AuditSpyForEndPointThatReceivesFromAnotherAndAuditsEndpoint() =>
            EndpointSetup<DefaultServer>();

        public class AuditMessageHandler(Context testContext) : IHandleMessages<OutgoingTestMessage>
        {
            public Task Handle(OutgoingTestMessage message, IMessageHandlerContext context)
            {
                testContext.MessageAudited = true;

                if (context.Extensions.TryGet<IncomingMessage>(out var incomingMessage) && incomingMessage.ReceiveProperties.TryGetValue("LearningTransport.FileCreatedAt", out var fileCreatedAt))
                {
                    if (fileCreatedAt == testContext.ReceiveFileCreatedAt)
                    {
                        testContext.MarkAsFailed(new Exception("Receive properties from the original message is propagated to audit messages."));
                    }

                    testContext.MarkAsCompleted(testContext.MessageAudited, testContext.ReceiveFileCreatedAt != fileCreatedAt);
                }
                else
                {
                    testContext.MarkAsFailed(new Exception("Failed to propagate receive properties from the original message."));
                }

                return Task.CompletedTask;
            }
        }
    }

    class SendingEndpoint : EndpointConfigurationBuilder
    {
        public SendingEndpoint() => EndpointSetup<DefaultServer>();

        class TestMessageHandler(Context testContext) : IHandleMessages<TestMessage>
        {
            public async Task Handle(TestMessage message, IMessageHandlerContext context)
            {
                testContext.MessageReceived = true;
                if (context.Extensions.TryGet<IncomingMessage>(out var incomingMessage) && incomingMessage.ReceiveProperties.TryGetValue("LearningTransport.FileCreatedAt", out var fileCreatedAt))
                {
                    testContext.ReceiveFileCreatedAt = fileCreatedAt;

                    await context.SendLocal(new OutgoingTestMessage());
                }
                else
                {
                    testContext.MarkAsFailed(new Exception("Failed to retrieve receive properties from the message context."));
                }
            }
        }

        //handler for the outgoing message to verify that receive properties are not propagated to outgoing messages
        class OutgoingTestMessageHandler(Context testContext) : IHandleMessages<OutgoingTestMessage>
        {
            public Task Handle(OutgoingTestMessage message, IMessageHandlerContext context)
            {
                testContext.MessageReceived = true;
                if (context.Extensions.TryGet<IncomingMessage>(out var incomingMessage) && incomingMessage.ReceiveProperties.TryGetValue("LearningTransport.FileCreatedAt", out var fileCreatedAt))
                {
                    testContext.SendFileCreatedAt = fileCreatedAt;
                    if (fileCreatedAt == testContext.ReceiveFileCreatedAt)
                    {
                        testContext.MarkAsFailed(new Exception("Receive properties from the original message is propagated to outgoing messages."));
                    }

                    testContext.MarkAsCompleted(testContext.MessageReceived, testContext.ReceiveFileCreatedAt != fileCreatedAt);
                }
                else
                {
                    testContext.MarkAsFailed(new Exception("Failed to retrieve receive properties from the message context."));
                }
                return Task.CompletedTask;
            }
        }
    }

    class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() => EndpointSetup<DefaultServer>(endpointConfiguration =>
        {
            endpointConfiguration.AuditProcessedMessagesTo(Conventions.EndpointNamingConvention(typeof(AuditSpy)));
        });

        class TestMessageHandler(Context testContext) : IHandleMessages<TestMessage>
        {
            public Task Handle(TestMessage message, IMessageHandlerContext context)
            {
                testContext.MessageReceived = true;

                if (context.Extensions.TryGet<IncomingMessage>(out var incomingMessage) && incomingMessage.ReceiveProperties.TryGetValue("LearningTransport.FileCreatedAt", out var fileCreatedAt))
                {
                    testContext.ReceiveFileCreatedAt = fileCreatedAt;
                }
                else
                {
                    testContext.MarkAsFailed(new Exception("Failed to retrieve receive properties from the message context."));
                }

                testContext.MarkAsCompleted(testContext.MessageReceived, testContext.ReceiveFileCreatedAt != null);

                return Task.CompletedTask;
            }
        }
    }

    class FailingEndpoint : EndpointConfigurationBuilder
    {
        public FailingEndpoint() => EndpointSetup<DefaultServer>(endpointConfiguration =>
        {
            endpointConfiguration.Recoverability().Delayed(settings => settings.NumberOfRetries(1));
            endpointConfiguration.Recoverability().Immediate(settings => settings.NumberOfRetries(0));
        });

        class TestMessageHandler(Context testContext) : IHandleMessages<TestMessage>
        {
            public Task Handle(TestMessage message, IMessageHandlerContext context)
            {
                if (testContext.NumberOfRetries == 0)
                {
                    if (context.Extensions.TryGet<IncomingMessage>(out var incomingMessage) && incomingMessage.ReceiveProperties.TryGetValue("LearningTransport.FileCreatedAt", out var fileCreatedAt))
                    {
                        testContext.ReceiveFileCreatedAt = fileCreatedAt;
                    }
                }
                else
                {
                    if (context.Extensions.TryGet<IncomingMessage>(out var incomingMessage) && incomingMessage.ReceiveProperties.TryGetValue("LearningTransport.FileCreatedAt", out var fileCreatedAt))
                    {
                        testContext.RetryFileCreatedAt = fileCreatedAt;
                        testContext.MarkAsCompleted(testContext.ReceiveFileCreatedAt == testContext.RetryFileCreatedAt);
                    }
                }
                testContext.NumberOfRetries++;

                throw new SimulatedException("Simulating an exception to see if it preserves receive properties on retries");
            }
        }
    }

    class EndpointWithAuditOn : EndpointConfigurationBuilder
    {
        public EndpointWithAuditOn() => EndpointSetup<DefaultServer>(endpointConfiguration =>
        {
            endpointConfiguration.AuditProcessedMessagesTo(Conventions.EndpointNamingConvention(typeof(AuditSpy)));
        });

        class TestMessageHandler(Context testContext) : IHandleMessages<TestMessage>
        {
            public Task Handle(TestMessage message, IMessageHandlerContext context)
            {
                testContext.MessageReceived = true;

                if (context.Extensions.TryGet<IncomingMessage>(out var incomingMessage) && incomingMessage.ReceiveProperties.TryGetValue("LearningTransport.FileCreatedAt", out var fileCreatedAt))
                {
                    testContext.ReceiveFileCreatedAt = fileCreatedAt;
                }

                return Task.CompletedTask;
            }
        }
    }

    class AuditSpy : EndpointConfigurationBuilder
    {
        public AuditSpy() =>
            EndpointSetup<DefaultServer>();

        public class AuditMessageHandler(Context testContext) : IHandleMessages<TestMessage>
        {
            public Task Handle(TestMessage message, IMessageHandlerContext context)
            {
                testContext.MessageAudited = true;

                if (context.Extensions.TryGet<IncomingMessage>(out var incomingMessage) && incomingMessage.ReceiveProperties.TryGetValue("LearningTransport.FileCreatedAt", out var fileCreatedAt))
                {
                    if (fileCreatedAt != testContext.ReceiveFileCreatedAt)
                    {
                        testContext.MarkAsFailed(new Exception("Receive properties from the original message is not propagated to audit messages."));
                    }

                    testContext.MarkAsCompleted(testContext.MessageAudited, testContext.ReceiveFileCreatedAt == fileCreatedAt);
                }
                else
                {
                    testContext.MarkAsFailed(new Exception("Failed to propagate receive properties from the original message."));
                }

                return Task.CompletedTask;
            }
        }
    }

    class EndpointWithFailingHandler : EndpointConfigurationBuilder
    {
        public EndpointWithFailingHandler() => EndpointSetup<DefaultServer>(endpointConfiguration =>
        {
            endpointConfiguration.Recoverability().AddUnrecoverableException<SimulatedException>();
            endpointConfiguration.SendFailedMessagesTo(Conventions.EndpointNamingConvention(typeof(ErrorSpy)));
        });

        class TestMessageHandler(Context testContext) : IHandleMessages<TestMessage>
        {
            public Task Handle(TestMessage message, IMessageHandlerContext context)
            {
                testContext.MessageReceived = true;

                if (context.Extensions.TryGet<IncomingMessage>(out var incomingMessage) && incomingMessage.ReceiveProperties.TryGetValue("LearningTransport.FileCreatedAt", out var fileCreatedAt))
                {
                    testContext.ReceiveFileCreatedAt = fileCreatedAt;
                }

                throw new SimulatedException("Message should be moved to error queue");
            }
        }
    }

    class EndpointWithFailingHandlerAndDispatchOverride : EndpointConfigurationBuilder
    {
        public EndpointWithFailingHandlerAndDispatchOverride() => EndpointSetup<DefaultServer>(endpointConfiguration =>
        {
            endpointConfiguration.Recoverability().AddUnrecoverableException<SimulatedException>();
            endpointConfiguration.SendFailedMessagesTo(Conventions.EndpointNamingConvention(typeof(ErrorSpyWithDispatchPropertyVerification)));
            endpointConfiguration.Pipeline.Register(behavior: new ErrorQueueDispatchPropertyOverrideBehavior(), description: "Override FileCreatedAt dispatch property on error queue messages");
        });

        class TestMessageHandler(Context testContext) : IHandleMessages<TestMessage>
        {
            public Task Handle(TestMessage message, IMessageHandlerContext context)
            {
                testContext.MessageReceived = true;

                if (context.Extensions.TryGet<IncomingMessage>(out var incomingMessage) && incomingMessage.ReceiveProperties.TryGetValue("LearningTransport.FileCreatedAt", out var fileCreatedAt))
                {
                    testContext.ReceiveFileCreatedAt = fileCreatedAt;
                }

                throw new SimulatedException("Message should be moved to error queue");
            }
        }
    }

    class ErrorQueueDispatchPropertyOverrideBehavior : Behavior<IRecoverabilityContext>
    {
        public override Task Invoke(IRecoverabilityContext context, Func<Task> next)
        {
            if (context.RecoverabilityAction is MoveToError)
            {
                context.RecoverabilityAction = new CustomMoveToError(context.RecoverabilityConfiguration.Failed.ErrorQueue);
            }

            return next();
        }

        class CustomMoveToError(string errorQueue) : MoveToError(errorQueue)
        {
            public override IReadOnlyCollection<IRoutingContext> GetRoutingContexts(IRecoverabilityActionContext context)
            {
                var routingContexts = base.GetRoutingContexts(context);

                foreach (var routingContext in routingContexts)
                {
                    routingContext.Extensions.GetOrCreate<DispatchProperties>()["LearningTransport.FileCreatedAt"] = DateTime.UtcNow.AddDays(10).ToString("o");
                }

                return routingContexts;
            }
        }
    }

    class ErrorSpy : EndpointConfigurationBuilder
    {
        public ErrorSpy() => EndpointSetup<DefaultServer>();

        public class ErrorMessageHandler(Context testContext) : IHandleMessages<TestMessage>
        {
            public Task Handle(TestMessage message, IMessageHandlerContext context)
            {
                testContext.MessageMovedToErrorQueue = true;

                if (context.Extensions.TryGet<IncomingMessage>(out var incomingMessage) && incomingMessage.ReceiveProperties.TryGetValue("LearningTransport.FileCreatedAt", out var fileCreatedAt))
                {
                    testContext.ReceiveFileCreatedAt = fileCreatedAt;
                    testContext.MarkAsCompleted(testContext.MessageMovedToErrorQueue, testContext.ReceiveFileCreatedAt != null);
                }
                else
                {
                    testContext.MarkAsFailed(new Exception("Failed to propagate receive properties to error queue message."));
                }

                return Task.CompletedTask;
            }
        }
    }

    class ErrorSpyWithDispatchPropertyVerification : EndpointConfigurationBuilder
    {
        public ErrorSpyWithDispatchPropertyVerification() => EndpointSetup<DefaultServer>();

        public class ErrorMessageHandler(Context testContext) : IHandleMessages<TestMessage>
        {
            public Task Handle(TestMessage message, IMessageHandlerContext context)
            {
                testContext.MessageMovedToErrorQueue = true;

                if (context.Extensions.TryGet<IncomingMessage>(out var incomingMessage) && incomingMessage.ReceiveProperties.TryGetValue("LearningTransport.FileCreatedAt", out var fileCreatedAt))
                {
                    if (fileCreatedAt == testContext.ReceiveFileCreatedAt)
                    {
                        testContext.MarkAsFailed(new Exception("Receive properties from the original message were propagated to error queue message instead of dispatch properties."));
                    }

                    testContext.ErrorQueueFileCreatedAtDiffersFromOriginal = testContext.ReceiveFileCreatedAt != fileCreatedAt;
                    testContext.MarkAsCompleted(testContext.MessageMovedToErrorQueue, testContext.ErrorQueueFileCreatedAtDiffersFromOriginal);
                }
                else
                {
                    testContext.MarkAsFailed(new Exception("Failed to retrieve receive properties from error queue message."));
                }

                return Task.CompletedTask;
            }
        }
    }

    public class TestMessage : IMessage;

    public class OutgoingTestMessage : IMessage;
}