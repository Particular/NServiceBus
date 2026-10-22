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

// A send from a handler goes through MessageOperations, which logs a failed operation right away, as a warning,
// because outside the message processing pipeline nothing else would. Inside a handler the exception then escapes
// the handler and recoverability logs it again with its decision: the failure is logged twice, at two levels, and
// the recoverability span links to the handler span, not to the send span.
public class When_send_fails_inside_a_handler : OpenTelemetryAcceptanceTest
{
    [Test]
    public async Task Should_log_the_failed_send_and_the_recoverability_decision_and_link_to_the_handler_span()
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
        var sendActivity = NServiceBusActivityListener.CompletedActivities.GetSendMessageActivities().Single(a => a.Status == ActivityStatusCode.Error);
        var recoverabilityActivity = NServiceBusActivityListener.CompletedActivities.GetRecoverabilityActivities().Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exceptionLogs.Select(l => (l.LoggerName, l.Level)), Is.EquivalentTo(ExpectedLogs));
            Assert.That(recoverabilityActivity.Links.Select(l => l.Context.SpanId), Does.Contain(handlerActivity.SpanId));
            Assert.That(recoverabilityActivity.Links.Select(l => l.Context.SpanId), Does.Not.Contain(sendActivity.SpanId));
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

    const string ErrorMessage = "send failed!";
    static readonly (string, Logging.LogLevel)[] ExpectedLogs =
    [
        ("NServiceBus.MessageOperations", Logging.LogLevel.Warn),
        ("NServiceBus.MoveToError", Logging.LogLevel.Error)
    ];
}
