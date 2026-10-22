namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Traces;

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting;
using Configuration.AdvancedExtensibility;
using EndpointTemplates;
using NServiceBus;
using NServiceBus.Pipeline;
using NUnit.Framework;

// A send started from a handler fails the message processing. The exception is recorded once on the spans, on the
// send span it escaped from first, and logged once, by recoverability with its decision. The recoverability span links
// to the handler span: the processing failed, not only the send.
public class When_send_fails_inside_a_handler : OpenTelemetryAcceptanceTest
{
    [Test]
    public async Task Should_record_and_log_the_exception_once_and_link_to_the_handler_span()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<SendingEndpoint>(e => e
                .CustomConfig(c => c.Recoverability().Immediate(i => i.NumberOfRetries(0)).Delayed(d => d.NumberOfRetries(0)))
                .DoNotFailOnErrorMessages()
                .When(s => s.SendLocal(new TriggerMessage())))
            .Done(c => !c.FailedMessages.IsEmpty)
            .Run();

        var exceptionLogs = context.Logs.Where(l => l.Level > Logging.LogLevel.Debug && l.Message.Contains(ErrorMessage)).ToList();

        var handlerActivity = NServiceBusActivityListener.CompletedActivities.GetInvokedHandlerActivities().Single();
        var incomingActivity = NServiceBusActivityListener.CompletedActivities.GetReceiveMessageActivities().Single(a => a.Status == ActivityStatusCode.Error);
        var sendActivity = NServiceBusActivityListener.CompletedActivities.GetSendMessageActivities().Single(a => a.Status == ActivityStatusCode.Error);
        var recoverabilityActivity = NServiceBusActivityListener.CompletedActivities.GetRecoverabilityActivities().Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exceptionLogs.Select(l => (l.LoggerName, l.Level)), Is.EquivalentTo(ExpectedLogs), "the exception should be logged once, with the recoverability decision");

            Assert.That(sendActivity.Events.Select(e => e.Name), Is.EqualTo(ExceptionEvent), "the event should be on the span the exception escaped from first");
            Assert.That(handlerActivity.Events.Where(e => e.Name == "exception"), Is.Empty);
            Assert.That(incomingActivity.Events.Where(e => e.Name == "exception"), Is.Empty);
            Assert.That(handlerActivity.Status, Is.EqualTo(ActivityStatusCode.Error));

            Assert.That(recoverabilityActivity.Links.Select(l => l.Context.SpanId), Does.Contain(handlerActivity.SpanId));
            Assert.That(recoverabilityActivity.Links.Select(l => l.Context.SpanId), Does.Not.Contain(sendActivity.SpanId));
        }
    }

    public class Context : ScenarioContext;

    public class SendingEndpoint : EndpointConfigurationBuilder
    {
        // "logs/dup" selects the span-and-logs mode, which adds the exception events.
        public SendingEndpoint() => EndpointSetup<DefaultServer>(c =>
        {
            c.GetSettings().Set($"ACCEPTANCETEST_ENV:{OptInEnvironmentVariable}", "logs/dup");
            c.Pipeline.Register(new FailingSendBehavior(), "fails sending FailingSendMessage");
        });

        [Handler]
        public class TriggerMessageHandler : IHandleMessages<TriggerMessage>
        {
            public Task Handle(TriggerMessage message, IMessageHandlerContext context) => context.SendLocal(new FailingSendMessage());
        }

        class FailingSendBehavior : Behavior<IOutgoingLogicalMessageContext>
        {
            public override Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next)
                => context.Message.MessageType == typeof(FailingSendMessage) ? throw new SimulatedException(ErrorMessage) : next();
        }
    }

    public class TriggerMessage : IMessage;

    public class FailingSendMessage : IMessage;

    const string OptInEnvironmentVariable = "OTEL_SEMCONV_EXCEPTION_SIGNAL_OPT_IN";
    const string ErrorMessage = "send failed!";
    static readonly string[] ExceptionEvent = ["exception"];
    static readonly (string, Logging.LogLevel)[] ExpectedLogs = [("NServiceBus.MoveToError", Logging.LogLevel.Error)];
}
