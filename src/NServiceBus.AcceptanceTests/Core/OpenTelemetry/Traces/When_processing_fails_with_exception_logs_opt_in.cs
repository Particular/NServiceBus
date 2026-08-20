namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Traces;

using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting;
using Configuration.AdvancedExtensibility;
using EndpointTemplates;
using NServiceBus;
using NUnit.Framework;

// The OTEL_SEMCONV_EXCEPTION_SIGNAL_OPT_IN override is applied while the OpenTelemetryFeature defaults
// run, which is AFTER the endpoint's activity factory has already been built from the instrumentation
// options. The opt-in only takes effect when the activity factory and the settings share a single
// InstrumentationOptions instance.
public class When_processing_fails_with_exception_logs_opt_in : OpenTelemetryAcceptanceTest
{
    [Test]
    public async Task Should_record_the_exception_as_a_log_instead_of_a_span_event()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<FailingEndpoint>(e => e
                .DoNotFailOnErrorMessages()
                .When(s => s.SendLocal(new FailingMessage())))
            .Run();

        Assert.That(context.FailedMessages, Has.Count.EqualTo(1), "the message should have failed");

        var handlerActivity = NServiceBusActivityListener.CompletedActivities.GetInvokedHandlerActivities().Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(handlerActivity.Events, Is.Empty, "the exception should not be recorded as a span event when the endpoint opted in to exceptions as logs");
            Assert.That(context.Logs.Any(l => l.LoggerName == "NServiceBus.ActivityFactory" && l.Level == Logging.LogLevel.Error && l.Message.Contains(ErrorMessage)), Is.True, "the exception should be recorded as an error log instead");
        }
    }

    public class Context : ScenarioContext;

    public class FailingEndpoint : EndpointConfigurationBuilder
    {
        // Does not call endpointConfiguration.Tracing(): the instrumentation options only come into existence while the endpoint is being created.
        public FailingEndpoint() => EndpointSetup<DefaultServer>(c => c.GetSettings().Set($"ACCEPTANCETEST_ENV:{OptInEnvironmentVariable}", "logs"));

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
}