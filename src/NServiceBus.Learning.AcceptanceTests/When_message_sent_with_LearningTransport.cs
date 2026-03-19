namespace NServiceBus.AcceptanceTests;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;
using Transport;

public class When_message_sent_with_LearningTransport : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_preserve_file_created_time_as_receive_property()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(b => b.When(session => session.SendLocal(new TestMessage())))
            .Done(c => c.MessageReceived)
            .Run();

        Assert.That(context.MessageReceived, Is.True, "Message was not received");
        Assert.That(context.FileCreatedAt, Is.Not.Null, "FileCreatedAt property should be present");
        Assert.That(DateTime.TryParse(context.FileCreatedAt, out _), Is.True, "FileCreatedAt should be a valid datetime");
    }

    class Context : ScenarioContext
    {
        public bool MessageReceived { get; set; }
        public string FileCreatedAt { get; set; }
    }

    class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() => EndpointSetup<DefaultServer>();

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

                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class TestMessage : IMessage { }
}