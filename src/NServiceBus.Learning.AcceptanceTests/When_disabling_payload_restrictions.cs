namespace NServiceBus.AcceptanceTests;

using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_disabling_payload_restrictions : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_allow_messages_above_64kb()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<LargePayloadEndpoint>(b => b.When(session => session.SendLocal(new SomeMessage
            {
                LargeProperty = new byte[1024 * 64]
            })))
            .Run();

        Assert.That(context.MessageReceived, Is.True, "Message was not received");
    }

    class Context : ScenarioContext
    {
        public bool MessageReceived { get; set; }
    }

    class LargePayloadEndpoint : EndpointConfigurationBuilder
    {
        public LargePayloadEndpoint() =>
            EndpointSetup<DefaultServer>(c =>
            {
                var transport = c.ConfigureTransport<LearningTransport>();
                transport.RestrictPayloadSize = false;
            });

        class SomeMessageHandler(Context testContext) : IHandleMessages<SomeMessage>
        {
            public Task Handle(SomeMessage message, IMessageHandlerContext context)
            {
                testContext.MessageReceived = true;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class SomeMessage : IMessage
    {
        public byte[] LargeProperty { get; set; }
    }
}