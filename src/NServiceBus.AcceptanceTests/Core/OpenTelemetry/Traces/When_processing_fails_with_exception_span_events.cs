namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Traces;

using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting;
using Configuration.AdvancedExtensibility;
using EndpointTemplates;
using NServiceBus;
using NUnit.Framework;

// The SpanAndLogs counterpart of When_processing_fails_with_exception_logs_opt_in: "logs/dup" selects SpanAndLogs.
public class When_processing_fails_with_exception_span_events : OpenTelemetryAcceptanceTest
{
    [Test]
    public async Task Should_record_the_exception_on_the_handler_span_and_log_it_with_the_decision_only()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<FailingEndpoint>(e => e
                .DoNotFailOnErrorMessages()
                .When(s => s.SendLocal(new FailingMessage())))
            .Run();

        Assert.That(context.FailedMessages, Has.Count.EqualTo(1), "the message should have failed");

        var handlerActivity = NServiceBusActivityListener.CompletedActivities.GetInvokedHandlerActivities().Single();
        var incomingActivity = NServiceBusActivityListener.CompletedActivities.GetReceiveMessageActivities().Single();

        // Debug entries are excluded: the dispatch connector dumps the outgoing fault headers, which carry the
        // exception message too.
        var exceptionLogs = context.Logs.Where(l => l.Level > Logging.LogLevel.Debug && l.Message.Contains(ErrorMessage)).ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(handlerActivity.Events.Select(e => e.Name), Is.EqualTo(ExceptionEvent), "the exception should be recorded on the innermost span");
            Assert.That(incomingActivity.Events.Where(e => e.Name == "exception"), Is.Empty, "the spans further out should only mark their status");
            Assert.That(exceptionLogs, Has.Count.EqualTo(1), "the exception should be logged exactly once");
            Assert.That(exceptionLogs.Single().LoggerName, Is.EqualTo("NServiceBus.MoveToError"), "the exception should be logged with the recoverability decision");
            Assert.That(exceptionLogs.Single().Level, Is.EqualTo(Logging.LogLevel.Error));
        }
    }

    public class Context : ScenarioContext;

    public class FailingEndpoint : EndpointConfigurationBuilder
    {
        public FailingEndpoint() => EndpointSetup<DefaultServer>(c => c.GetSettings().Set($"ACCEPTANCETEST_ENV:{OptInEnvironmentVariable}", "logs/dup"));

        [Handler]
        public class FailingMessageHandler(Context testContext) : IHandleMessages<FailingMessage>
        {
            public Task Handle(FailingMessage message, IMessageHandlerContext context)
            {
                testContext.MarkAsCompleted();
                throw new SimulatedException(ErrorMessage);
            }
        }
    }

    public class FailingMessage : IMessage;

    const string OptInEnvironmentVariable = "OTEL_SEMCONV_EXCEPTION_SIGNAL_OPT_IN";
    const string ErrorMessage = "boom!";
    static readonly string[] ExceptionEvent = ["exception"];
}
