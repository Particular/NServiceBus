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

        var recoverabilityFailureLogs = context.Logs.Where(l => ReferenceEquals(l.Exception, context.RecoverabilityException)).ToList();
        var failedRecoverabilityActivity = NServiceBusActivityListener.CompletedActivities.GetRecoverabilityActivities().OrderBy(a => a.StartTimeUtc).First();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(recoverabilityFailureLogs.Select(l => l.Level), Is.EqualTo([Logging.LogLevel.Fatal]), "the critical error should log it, once");

            Assert.That(failedRecoverabilityActivity.Status, Is.EqualTo(ActivityStatusCode.Error), "the span of the failed first attempt");
        }
    }

    public class Context : ScenarioContext
    {
        public bool CriticalErrorRaised { get; set; }
        public int RecoverabilityAttempts;
        public Exception RecoverabilityException { get; set; }
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
                    testContext.RecoverabilityException = new SimulatedException();
                    throw testContext.RecoverabilityException;
                }

                return next();
            }
        }
    }

    public class FailingMessage : IMessage;
}
