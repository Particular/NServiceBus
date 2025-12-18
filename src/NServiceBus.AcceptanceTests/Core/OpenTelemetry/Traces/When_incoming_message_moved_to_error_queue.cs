namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Traces;

using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting.Support;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NUnit.Framework;

public class When_incoming_message_moved_to_error_queue : OpenTelemetryAcceptanceTest
{
    [Test]
    public void Should_add_start_new_trace_header()
    {
        var exception = Assert.CatchAsync<MessageFailedException>(async () =>
        {
            await Scenario.Define<Context>()
                .WithEndpoint<FailingEndpoint>(e => e
                    .When(s => s.SendLocal(new FailingMessage())))
                .Run();
        });

        var failedMessage = exception.FailedMessage;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(failedMessage.Headers.ContainsKey(Headers.StartNewTrace), Is.True);
            Assert.That(failedMessage.Headers[Headers.StartNewTrace], Is.EqualTo(bool.TrueString));
        }
    }

    class Context : ScenarioContext
    {
        public bool HandlerInvoked { get; set; }
    }

    class FailingEndpoint : EndpointConfigurationBuilder
    {
        public FailingEndpoint() => EndpointSetup<OpenTelemetryEnabledEndpoint>();

        class FailingMessageHandler : IHandleMessages<FailingMessage>
        {

            Context textContext;

            public FailingMessageHandler(Context textContext) => this.textContext = textContext;

            public Task Handle(FailingMessage message, IMessageHandlerContext context)
            {
                textContext.HandlerInvoked = true;
                throw new SimulatedException(ErrorMessage);
            }
        }
    }

    public class FailingMessage : IMessage
    {
    }

    const string ErrorMessage = "oh no!";
}