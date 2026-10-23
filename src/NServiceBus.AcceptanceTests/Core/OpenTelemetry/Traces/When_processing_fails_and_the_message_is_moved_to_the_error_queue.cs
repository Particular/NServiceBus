namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Traces;

using System;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NServiceBus;
using NUnit.Framework;

public class When_processing_fails_and_the_message_is_moved_to_the_error_queue : OpenTelemetryAcceptanceTest
{
    [TestCase(ExceptionRecordingMode.Logs)]
    [TestCase(ExceptionRecordingMode.SpanAndLogs)]
    public async Task Should_log_the_exception_once_with_the_decision_under_the_recoverability_span(ExceptionRecordingMode exceptionRecordingMode)
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<FailingEndpoint>(e => e
                .CustomConfig(c =>
                {
                    c.Tracing().ExceptionRecordingMode = exceptionRecordingMode;
                    c.Recoverability().Immediate(i => i.NumberOfRetries(0)).Delayed(d => d.NumberOfRetries(0));
                })
                .DoNotFailOnErrorMessages()
                .When(s => s.SendLocal(new FailingMessage())))
            .Done(c => !c.FailedMessages.IsEmpty)
            .Run();

        var recoverabilityActivity = NServiceBusActivityListener.CompletedActivities.GetRecoverabilityActivities().Single();
        var exceptionLog = context.Logs.Single(l => ReferenceEquals(l.Exception, context.HandlerException));
        var handlerActivity = NServiceBusActivityListener.CompletedActivities.GetInvokedHandlerActivities().Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exceptionLog.Level, Is.EqualTo(Logging.LogLevel.Error), "a move to the error queue is logged at Error");
            Assert.That(exceptionLog.SpanId, Is.EqualTo(recoverabilityActivity.SpanId), "the log entry should be correlated with the recoverability span");

            Assert.That(recoverabilityActivity.Links.Select(l => l.Context.SpanId), Is.EqualTo([handlerActivity.SpanId]), "the recoverability span should link to the handler span");
        }
    }

    public class Context : ScenarioContext
    {
        public Exception HandlerException { get; set; }
    }

    public class FailingEndpoint : EndpointConfigurationBuilder
    {
        public FailingEndpoint() => EndpointSetup<DefaultServer>();

        [Handler]
        public class FailingMessageHandler(Context testContext) : IHandleMessages<FailingMessage>
        {
            public Task Handle(FailingMessage message, IMessageHandlerContext context)
            {
                testContext.HandlerException = new SimulatedException(ErrorMessage);
                throw testContext.HandlerException;
            }
        }
    }

    public class FailingMessage : IMessage;

    const string ErrorMessage = "handler failed!";
}