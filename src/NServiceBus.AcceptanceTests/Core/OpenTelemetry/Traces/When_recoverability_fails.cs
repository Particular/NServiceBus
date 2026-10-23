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

        var recoverabilityFailureLog = context.Logs.Single(l => l is { Level: > Logging.LogLevel.Debug, Message: not null } && l.Message.Contains(RecoverabilityErrorMessage));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(recoverabilityFailureLog.Level, Is.EqualTo(Logging.LogLevel.Fatal), "the critical error should log it");
            Assert.That(recoverabilityFailureLog.Exception, Is.SameAs(context.RecoverabilityException));

            Assert.That(context.ActivityWhenRecoverabilityFailed.OperationName, Is.EqualTo("NServiceBus.Diagnostics.Recoverability"), "the failure should happen inside the recoverability span");
            Assert.That(context.ActivityWhenRecoverabilityFailed.Status, Is.EqualTo(ActivityStatusCode.Error));
        }
    }

    public class Context : ScenarioContext
    {
        public bool CriticalErrorRaised { get; set; }
        public int RecoverabilityAttempts;
        public Exception RecoverabilityException { get; set; }
        public Activity ActivityWhenRecoverabilityFailed { get; set; }
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
            {
                if (Interlocked.Increment(ref testContext.RecoverabilityAttempts) == 1)
                {
                    testContext.RecoverabilityException = new SimulatedException(RecoverabilityErrorMessage);
                    testContext.ActivityWhenRecoverabilityFailed = Activity.Current;
                    throw testContext.RecoverabilityException;
                }

                return next();
            }
        }
    }

    public class FailingMessage : IMessage;

    const string RecoverabilityErrorMessage = "recoverability failed!";
}