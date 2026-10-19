namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Traces;

using System.Collections.Generic;
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
        }

        var exceptionLogs = ExceptionLogs(context);
        Assert.That(exceptionLogs, Has.Count.EqualTo(1), "the exception should be logged once");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(exceptionLogs[0].LoggerName, Is.EqualTo("NServiceBus.MoveToError"), "the recoverability pipeline handles the exception, so it logs it");
            Assert.That(exceptionLogs[0].Level, Is.EqualTo(Logging.LogLevel.Error));
        }
    }

    [Test]
    public async Task Should_log_the_exception_at_the_level_of_the_recoverability_action()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<RetryingEndpoint>(e => e
                .When(s => s.SendLocal(new FailingMessage())))
            .Run();

        var exceptionLogs = ExceptionLogs(context);
        Assert.That(exceptionLogs, Has.Count.EqualTo(1), "the exception should be logged once");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(exceptionLogs[0].LoggerName, Is.EqualTo("NServiceBus.ImmediateRetry"));
            Assert.That(exceptionLogs[0].Level, Is.EqualTo(Logging.LogLevel.Info), "an immediate retry is not an error");
        }
    }

    // Debug logs of outgoing messages list the fault headers, which also contain the exception message.
    static List<ScenarioContext.LogItem> ExceptionLogs(Context context) =>
        context.Logs.Where(l => l.Level >= Logging.LogLevel.Info && l.Message?.Contains(ErrorMessage) == true).ToList();

    public class Context : ScenarioContext
    {
        public int InvocationCounter { get; set; }
    }

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

    public class RetryingEndpoint : EndpointConfigurationBuilder
    {
        public RetryingEndpoint() => EndpointSetup<DefaultServer>(c =>
        {
            c.GetSettings().Set($"ACCEPTANCETEST_ENV:{OptInEnvironmentVariable}", "logs");
            c.Recoverability().Immediate(i => i.NumberOfRetries(1));
        });

        [Handler]
        public class RetryingMessageHandler(Context testContext) : IHandleMessages<FailingMessage>
        {
            public Task Handle(FailingMessage message, IMessageHandlerContext context)
            {
                testContext.InvocationCounter++;

                if (testContext.InvocationCounter == 1)
                {
                    throw new SimulatedException(ErrorMessage);
                }

                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class FailingMessage : IMessage;

    const string OptInEnvironmentVariable = "OTEL_SEMCONV_EXCEPTION_SIGNAL_OPT_IN";
    const string ErrorMessage = "boom!";
}