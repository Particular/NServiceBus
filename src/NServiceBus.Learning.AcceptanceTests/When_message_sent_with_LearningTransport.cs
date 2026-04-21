namespace NServiceBus.AcceptanceTests;

using System;
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
            Assert.That(context.FileCreatedAt, Is.Not.Null, "FileCreatedAt property should be present");
            Assert.That(DateTime.TryParse(context.FileCreatedAt, out _), Is.True, "FileCreatedAt should be a valid datetime");
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
        }
    }

    [Test]
    public async Task Should_not_override_audit_properties_with_receive_properties_when_dispatch_properties_are_used()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<SendToAnotherEndpoint>(b => b.When(session => session.SendLocal(new TestMessage())))
            .WithEndpoint<EndPointThatReceivesFromAnotherAndAuditsEndpoint>()
            .WithEndpoint<AuditSpyForEndPointThatReceivesFromAnotherAndAuditsEndpoint>()
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.MessageReceived, Is.True, "Message was not received");
        }
    }

    class Context : ScenarioContext
    {
        public bool MessageReceived { get; set; }
        public string FileCreatedAt { get; set; }
        public bool MessageAudited { get; set; }
    }

    class SendToAnotherEndpoint : EndpointConfigurationBuilder
    {
        public SendToAnotherEndpoint() => EndpointSetup<DefaultServer>(e => e.ConfigureRouting().RouteToEndpoint(typeof(OutgoingTestMessage), Conventions.EndpointNamingConvention(typeof(EndPointThatReceivesFromAnotherAndAuditsEndpoint))));

        class TestMessageHandler(Context testContext) : IHandleMessages<TestMessage>
        {
            public async Task Handle(TestMessage message, IMessageHandlerContext context)
            {
                testContext.MessageReceived = true;

                var fileCreatedAtDispatchProperty = DateTime.UtcNow.AddDays(-10).ToString("o");
                testContext.FileCreatedAt = fileCreatedAtDispatchProperty;

                SendOptions sendOptions = new();
                sendOptions.SetHeader("LearningTransport.FileCreatedAt", fileCreatedAtDispatchProperty);
                await context.Send(new OutgoingTestMessage(), sendOptions);
            }
        }
    }

    class EndPointThatReceivesFromAnotherAndAuditsEndpoint : EndpointConfigurationBuilder
    {
        public EndPointThatReceivesFromAnotherAndAuditsEndpoint() => EndpointSetup<DefaultServer>(endpointConfiguration =>
        {
            endpointConfiguration.AuditProcessedMessagesTo(Conventions.EndpointNamingConvention(typeof(AuditSpyForEndPointThatReceivesFromAnotherAndAuditsEndpoint)));
            endpointConfiguration.Pipeline.Register(
                behavior: new AuditHeaderOverrideBehavior(),
                description: "Override headers on audit messages");
        });

        class OutgoingTestMessageHandler(Context testContext) : IHandleMessages<OutgoingTestMessage>
        {
            public async Task Handle(OutgoingTestMessage message, IMessageHandlerContext context)
            {
                testContext.MarkAsCompleted(testContext.MessageReceived);
            }
        }
    }

    class AuditHeaderOverrideBehavior : Behavior<IAuditContext>
    {
        public override Task Invoke(IAuditContext context, Func<Task> next)
        {
            // Option B: override via audit metadata (will be copied into headers by default audit action)
            context.AuditMetadata["LearningTransport.FileCreatedAt"] = DateTime.UtcNow.AddDays(10).ToString("o");

            return next();
        }
    }

    class AuditSpyForEndPointThatReceivesFromAnotherAndAuditsEndpoint : EndpointConfigurationBuilder
    {
        public AuditSpyForEndPointThatReceivesFromAnotherAndAuditsEndpoint() =>
            EndpointSetup<DefaultServer>();

        public class AuditMessageHandler(Context testContext) : IHandleMessages<TestMessage>
        {
            public Task Handle(TestMessage message, IMessageHandlerContext context)
            {
                testContext.MessageAudited = true;

                if (context.Extensions.TryGet<ReceiveProperties>(out var receiveProperties))
                {
                    if (receiveProperties.TryGetValue("LearningTransport.FileCreatedAt", out var fileCreatedAt))
                    {
                        if (fileCreatedAt == testContext.FileCreatedAt)
                        {
                            testContext.MarkAsFailed(new Exception("Receive properties from the original message is propagated to audit messages."));
                        }

                        testContext.MarkAsCompleted(testContext.MessageAudited, testContext.FileCreatedAt != fileCreatedAt);
                    }
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
                if (context.Extensions.TryGet<ReceiveProperties>(out var receiveProperties))
                {
                    receiveProperties.TryGetValue("LearningTransport.FileCreatedAt", out var fileCreatedAt);

                    testContext.FileCreatedAt = fileCreatedAt;

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
            public async Task Handle(OutgoingTestMessage message, IMessageHandlerContext context)
            {
                testContext.MessageReceived = true;
                if (context.Extensions.TryGet<ReceiveProperties>(out var receiveProperties))
                {
                    receiveProperties.TryGetValue("LearningTransport.FileCreatedAt", out var fileCreatedAt);

                    if (fileCreatedAt == testContext.FileCreatedAt)
                    {
                        testContext.MarkAsFailed(new Exception("Receive properties from the original message is propagated to outgoing messages."));
                    }

                    testContext.MarkAsCompleted(testContext.MessageReceived, testContext.FileCreatedAt != fileCreatedAt);
                }
                else
                {
                    testContext.MarkAsFailed(new Exception("Failed to retrieve receive properties from the message context."));
                }
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

                if (context.Extensions.TryGet<ReceiveProperties>(out var receiveProperties))
                {
                    if (receiveProperties.TryGetValue("LearningTransport.FileCreatedAt", out var fileCreatedAt))
                    {
                        testContext.FileCreatedAt = fileCreatedAt;
                    }
                }
                else
                {
                    testContext.MarkAsFailed(new Exception("Failed to retrieve receive properties from the message context."));
                }

                testContext.MarkAsCompleted(testContext.MessageReceived, testContext.FileCreatedAt != null);

                return Task.CompletedTask;
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

                if (context.Extensions.TryGet<ReceiveProperties>(out var receiveProperties))
                {
                    if (receiveProperties.TryGetValue("LearningTransport.FileCreatedAt", out var fileCreatedAt))
                    {
                        testContext.FileCreatedAt = fileCreatedAt;
                    }
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

                if (context.Extensions.TryGet<ReceiveProperties>(out var receiveProperties))
                {
                    if (receiveProperties.TryGetValue("LearningTransport.FileCreatedAt", out var fileCreatedAt))
                    {
                        if (fileCreatedAt != testContext.FileCreatedAt)
                        {
                            testContext.MarkAsFailed(new Exception("Receive properties from the original message is not propagated to audit messages."));
                        }

                        testContext.MarkAsCompleted(testContext.MessageAudited, testContext.FileCreatedAt == fileCreatedAt);
                    }
                }
                else
                {
                    testContext.MarkAsFailed(new Exception("Failed to propagate receive properties from the original message."));
                }

                return Task.CompletedTask;
            }
        }
    }

    public class TestMessage : IMessage
    {
    }

    public class OutgoingTestMessage : IMessage
    {
    }
}