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
        //The scenario is: send->receive->handle->publish->throw
        var context = await Scenario.Define<Context>()
            .WithEndpoint<PublishingEndpoint>(e => e
                .CustomConfig(c =>
                {
                    c.Tracing().ExceptionRecordingMode = exceptionRecordingMode;
                    c.Recoverability().Immediate(i => i.NumberOfRetries(0)).Delayed(d => d.NumberOfRetries(0));
                })
                .DoNotFailOnErrorMessages()
                .When(s => s.SendLocal(new TriggerMessage())))
            .Done(c => !c.FailedMessages.IsEmpty)
            .Run();

        var activities = NServiceBusActivityListener.CompletedActivities;

        Activity[] spansPassedThrough = [
            activities.GetPublishEventActivities().Single(),
            activities.GetInvokedHandlerActivities().Single(),
            activities.GetReceiveMessageActivities().Single()];

        var exceptionEvents = spansPassedThrough.SelectMany(a => a.Events.Where(e => e.Name == "exception").Select(_ => a)).ToList();
        var exceptionLogs = context.Logs.Where(l => ReferenceEquals(l.Exception, context.PublishException)).ToList();

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

    public class Context : ScenarioContext
    {
        public Exception PublishException { get; set; }
    }

    public class PublishingEndpoint : EndpointConfigurationBuilder
    {
        public PublishingEndpoint() => EndpointSetup<DefaultServer>(c => c.Pipeline.Register<FailingPublishBehavior>("fails publishing FailingEvent"));

        [Handler]
        public class TriggerMessageHandler : IHandleMessages<TriggerMessage>
        {
            public Task Handle(TriggerMessage message, IMessageHandlerContext context) => context.Publish(new FailingEvent());
        }

        public class FailingPublishBehavior(Context testContext) : Behavior<IOutgoingPublishContext>
        {
            public override Task Invoke(IOutgoingPublishContext context, Func<Task> next)
            {
                testContext.PublishException = new SimulatedException();
                throw testContext.PublishException;
            }
        }
    }

    public class TriggerMessage : IMessage;

    public class FailingEvent : IEvent;
}