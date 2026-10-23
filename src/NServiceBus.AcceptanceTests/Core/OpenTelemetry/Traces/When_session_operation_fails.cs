namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Traces;

using System;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NServiceBus;
using NServiceBus.Pipeline;
using NUnit.Framework;

public class When_session_operation_fails : OpenTelemetryAcceptanceTest
{
    [TestCase(ExceptionRecordingMode.Logs, true)]
    [TestCase(ExceptionRecordingMode.SpanAndLogs, true)]
    [TestCase(ExceptionRecordingMode.Logs, false, TestName = "Should log the exception as a warning without tracing")]
    public async Task Should_log_the_exception_as_a_warning_correlated_with_the_operation_span(ExceptionRecordingMode exceptionRecordingMode, bool tracingEnabled)
    {
        if (!tracingEnabled)
        {
            DisableTracing();
        }

        var context = await Scenario.Define<Context>()
            .WithEndpoint<SendingEndpoint>(e => e
                .CustomConfig(c => c.Tracing().ExceptionRecordingMode = exceptionRecordingMode)
                .When(async (session, ctx) =>
                {
                    try
                    {
                        await session.SendLocal(new FailingMessage());
                    }
                    catch (SimulatedException ex)
                    {
                        ctx.SendException = ex;
                    }
                }))
            .Done(c => c.SendException is not null)
            .Run();

        var exceptionLog = context.Logs.Single(l => l is { Level: > Logging.LogLevel.Debug, Message: not null } && l.Message.Contains(ErrorMessage));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exceptionLog.Level, Is.EqualTo(Logging.LogLevel.Warn));
            Assert.That(exceptionLog.Exception, Is.SameAs(context.SendException));
        }

        if (tracingEnabled)
        {
            var sendActivity = NServiceBusActivityListener.CompletedActivities.GetSendMessageActivities().Single();

            Assert.That(exceptionLog.SpanId, Is.EqualTo(sendActivity.SpanId), "the log entry should be correlated with the send span");
        }
    }

    public class Context : ScenarioContext
    {
        public Exception SendException { get; set; }
    }

    public class SendingEndpoint : EndpointConfigurationBuilder
    {
        public SendingEndpoint() => EndpointSetup<DefaultServer>(c => c.Pipeline.Register(new FailingSendBehavior(), "fails sending FailingMessage"));

        class FailingSendBehavior : Behavior<IOutgoingLogicalMessageContext>
        {
            public override Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next)
                => context.Message.MessageType == typeof(FailingMessage) ? throw new SimulatedException(ErrorMessage) : next();
        }

        [Handler]
        public class FailingMessageHandler : IHandleMessages<FailingMessage>
        {
            public Task Handle(FailingMessage message, IMessageHandlerContext context) => Task.CompletedTask;
        }
    }

    public class FailingMessage : IMessage;

    const string ErrorMessage = "session send failed!";
}