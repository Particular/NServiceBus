namespace NServiceBus.AcceptanceTests.Core.CriticalError;

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Features;
using NUnit.Framework;
using CriticalError = NServiceBus.CriticalError;

public class When_raising_critical_error_at_startup : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_call_critical_error_action_for_every_error_that_occurred_before_startup()
    {
        var context = await Scenario.Define<TestContext>()
            .WithEndpoint<EndpointWithCriticalErrorStartup>(b =>
                b.CustomConfig((config, ctx) => config.DefineCriticalErrorAction((errorContext, token) => CollectCriticalErrors(errorContext, ctx, token))))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.CriticalErrorsRaised, Is.EqualTo(2));
            Assert.That(context.Exceptions, Has.Count.EqualTo(context.CriticalErrorsRaised));
        }

        return;

        Task CollectCriticalErrors(ICriticalErrorContext criticalContext, TestContext testContext, CancellationToken cancellationToken)
        {
            testContext.Exceptions.TryAdd(criticalContext.Error, criticalContext.Exception);
            testContext.MarkAsCompleted(testContext.Exceptions.Count >= 2);
            return Task.CompletedTask;
        }
    }

    public class TestContext : ScenarioContext
    {
        public int CriticalErrorsRaised { get; set; }
        public ConcurrentDictionary<string, Exception> Exceptions { get; } = [];
    }

    public class EndpointWithCriticalErrorStartup : EndpointConfigurationBuilder
    {
        public EndpointWithCriticalErrorStartup() => EndpointSetup<DefaultServer>(c => c.RegisterStartupTask<CriticalErrorStartupFeatureTask>());

        class CriticalErrorStartupFeatureTask(CriticalError criticalError, TestContext testContext) : FeatureStartupTask
        {
            protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
            {
                criticalError.Raise("critical error 1", new SimulatedException(), cancellationToken);
                testContext.CriticalErrorsRaised++;

                criticalError.Raise("critical error 2", new SimulatedException(), cancellationToken);
                testContext.CriticalErrorsRaised++;

                return Task.CompletedTask;
            }

            protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }
    }
}