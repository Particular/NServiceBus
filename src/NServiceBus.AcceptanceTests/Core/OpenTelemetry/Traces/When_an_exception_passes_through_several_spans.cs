namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Traces;

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NServiceBus;
using NServiceBus.Pipeline;
using NUnit.Framework;

public class When_an_exception_passes_through_several_spans : OpenTelemetryAcceptanceTest
{
    [TestCase(ExceptionRecordingMode.Logs)]
    [TestCase(ExceptionRecordingMode.SpanAndLogs)]
    public async Task Should_record_the_exception_once_mark_every_span_as_failed_and_log_it_once(ExceptionRecordingMode exceptionRecordingMode)
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<SendingEndpoint>(e => e
                .CustomConfig(c =>
                {
                    c.Tracing().ExceptionRecordingMode = exceptionRecordingMode;
                    c.Recoverability().Immediate(i => i.NumberOfRetries(0)).Delayed(d => d.NumberOfRetries(0));
                })
                .DoNotFailOnErrorMessages()
                .When(s => s.SendLocal(new TriggerMessage())))
            .Done(c => !c.FailedMessages.IsEmpty)
            .Run();

        var handlerActivity = NServiceBusActivityListener.CompletedActivities.GetInvokedHandlerActivities().Single();

        Activity[] spansPassedThrough = [
            NServiceBusActivityListener.CompletedActivities.GetSendMessageActivities().Single(a => a.ParentSpanId == handlerActivity.SpanId),
            handlerActivity,
            NServiceBusActivityListener.CompletedActivities.GetReceiveMessageActivities().Single(a => a.SpanId == handlerActivity.ParentSpanId)];

        var exceptionEvents = spansPassedThrough.SelectMany(a => a.Events.Where(e => e.Name == "exception").Select(_ => a)).ToList();
        var exceptionLogs = context.Logs.Where(l => l is { Level: > Logging.LogLevel.Debug, Message: not null } && l.Message.Contains(ErrorMessage)).ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exceptionLogs, Has.Count.EqualTo(1), "logged once");

            Assert.That(spansPassedThrough.Select(a => a.Status), Is.All.EqualTo(ActivityStatusCode.Error), "every span the exception passed through");

            if (exceptionRecordingMode == ExceptionRecordingMode.SpanAndLogs)
            {
                Assert.That(exceptionEvents, Is.EqualTo([spansPassedThrough[0]]), "one event, on the innermost span");
            }
            else
            {
                Assert.That(exceptionEvents, Is.Empty, "no events without the span-and-logs mode");
            }
        }
    }

    public class Context : ScenarioContext;

    public class SendingEndpoint : EndpointConfigurationBuilder
    {
        public SendingEndpoint() => EndpointSetup<DefaultServer>(c => c.Pipeline.Register(new FailingSendBehavior(), "fails sending FailingSendMessage"));

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

    const string ErrorMessage = "send from handler failed!";
}