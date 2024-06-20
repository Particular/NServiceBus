namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Traces;

using System.Linq;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NUnit.Framework;

public class When_incoming_message_moved_to_error_queue : OpenTelemetryAcceptanceTest
{
    [Test]
    public async Task Should_add_start_new_trace_header()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<FailingEndpoint>(e => e
                .DoNotFailOnErrorMessages()
                .When(s => s.SendLocal(new FailingMessage())))
            .Done(c => c.FailedMessages.Count == 1)
            .Run();

        var failedMessage = context.FailedMessages.First().Value.First();
        Assert.IsTrue(failedMessage.Headers.ContainsKey(Headers.StartNewTrace));
        Assert.AreEqual(bool.TrueString, failedMessage.Headers[Headers.StartNewTrace]);
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