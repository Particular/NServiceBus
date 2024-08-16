namespace NServiceBus.AcceptanceTests.Recoverability;

using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Support;
using EndpointTemplates;
using NUnit.Framework;

public class When_message_fails_retries : NServiceBusAcceptanceTest
{
    [Test]
    public void Should_forward_message_to_error_queue()
    {
        var exception = Assert.ThrowsAsync<MessageFailedException>(async () => await Scenario.Define<Context>()
                .WithEndpoint<RetryEndpoint>(b => b
                    .When((session, c) => session.SendLocal(new MessageWhichFailsRetries())))
                .Done(c => !c.FailedMessages.IsEmpty)
                .Run());

        Assert.That(exception.ScenarioContext.FailedMessages.Count, Is.EqualTo(1));

        var testContext = (Context)exception.ScenarioContext;
        Assert.That(exception.FailedMessage.Headers[Headers.EnclosedMessageTypes], Is.EqualTo(typeof(MessageWhichFailsRetries).AssemblyQualifiedName));
        Assert.That(exception.FailedMessage.MessageId, Is.EqualTo(testContext.PhysicalMessageId));
        Assert.IsAssignableFrom(typeof(SimulatedException), exception.FailedMessage.Exception);

        Assert.That(testContext.Logs.Count(l => l.Message
            .StartsWith($"Moving message '{testContext.PhysicalMessageId}' to the error queue 'error' because processing failed due to an exception:")), Is.EqualTo(1));
    }

    public class RetryEndpoint : EndpointConfigurationBuilder
    {
        public RetryEndpoint()
        {
            EndpointSetup<DefaultServer>();
        }

        class MessageHandler : IHandleMessages<MessageWhichFailsRetries>
        {
            public MessageHandler(Context context)
            {
                testContext = context;
            }

            public Task Handle(MessageWhichFailsRetries message, IMessageHandlerContext context)
            {
                testContext.PhysicalMessageId = context.MessageId;
                throw new SimulatedException();
            }

            Context testContext;
        }
    }

    class Context : ScenarioContext
    {
        public string PhysicalMessageId { get; set; }
    }

    public class MessageWhichFailsRetries : IMessage
    {
    }
}