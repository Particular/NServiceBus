namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Traces;

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NServiceBus;
using NServiceBus.Pipeline;
using NUnit.Framework;

// Carrying out the recoverability decision fails once. The transport raises a critical error, which logs the failure
// at Fatal; recoverability does not log it as well. The message is processed again and moved to the error queue.
public class When_recoverability_fails : OpenTelemetryAcceptanceTest
{
    [Test]
    public async Task Should_log_the_failure_once_through_the_critical_error()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<FailingEndpoint>(e => e
                .CustomConfig((c, ctx) =>
                {
                    c.Recoverability().Immediate(i => i.NumberOfRetries(0)).Delayed(d => d.NumberOfRetries(0));
                    c.DefineCriticalErrorAction((_, _) =>
                    {
                        ctx.CriticalErrorRaised = true;
                        return Task.CompletedTask;
                    });
                })
                .DoNotFailOnErrorMessages()
                .When(s => s.SendLocal(new FailingMessage())))
            .Done(c => c.CriticalErrorRaised && !c.FailedMessages.IsEmpty)
            .Run();

        var recoverabilityFailureLogs = context.Logs.Where(l => l.Level > Logging.LogLevel.Debug && l.Message.Contains(RecoverabilityErrorMessage)).ToList();
        var failedRecoverabilityActivities = NServiceBusActivityListener.CompletedActivities.GetRecoverabilityActivities().Where(a => a.Status == ActivityStatusCode.Error).ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(recoverabilityFailureLogs, Has.Count.EqualTo(1), "the recoverability failure should be logged exactly once");
            Assert.That(recoverabilityFailureLogs.Single().Level, Is.EqualTo(Logging.LogLevel.Fatal), "the critical error should log it");
            Assert.That(failedRecoverabilityActivities, Has.Count.EqualTo(1), "the recoverability span should be marked as failed");
        }
    }

    public class Context : ScenarioContext
    {
        public bool CriticalErrorRaised { get; set; }
        public int RecoverabilityAttempts;
    }

    public class FailingEndpoint : EndpointConfigurationBuilder
    {
        public FailingEndpoint() => EndpointSetup<DefaultServer>(c => c.Pipeline.Register<FailOnceRecoverabilityBehavior>("fails carrying out the first recoverability decision"));

        [Handler]
        public class FailingMessageHandler : IHandleMessages<FailingMessage>
        {
            public Task Handle(FailingMessage message, IMessageHandlerContext context) => throw new SimulatedException("processing failed");
        }

        public class FailOnceRecoverabilityBehavior(Context testContext) : Behavior<IRecoverabilityContext>
        {
            public override Task Invoke(IRecoverabilityContext context, Func<Task> next)
                => Interlocked.Increment(ref testContext.RecoverabilityAttempts) == 1 ? throw new SimulatedException(RecoverabilityErrorMessage) : next();
        }
    }

    public class FailingMessage : IMessage;

    const string RecoverabilityErrorMessage = "recoverability failed!";
}
