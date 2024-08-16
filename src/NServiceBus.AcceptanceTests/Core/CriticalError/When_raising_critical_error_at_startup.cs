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
        var exceptions = new ConcurrentDictionary<string, Exception>();

        Func<ICriticalErrorContext, CancellationToken, Task> addCritical = (criticalContext, _) =>
        {
            exceptions.TryAdd(criticalContext.Error, criticalContext.Exception);
            return Task.CompletedTask;
        };

        var context = await Scenario.Define<TestContext>()
            .WithEndpoint<EndpointWithCriticalErrorStartup>(b =>
                b.CustomConfig(config => config.DefineCriticalErrorAction(addCritical)))
            .Done(c => c.CriticalErrorsRaised >= 2 && exceptions.Count >= 2)
            .Run();

        Assert.That(context.CriticalErrorsRaised, Is.EqualTo(2));
        Assert.That(exceptions.Count, Is.EqualTo(context.CriticalErrorsRaised));
    }

    public class TestContext : ScenarioContext
    {
        public string ContextId { get; set; }
        public int CriticalErrorsRaised { get; set; }
    }

    public class EndpointWithCriticalError : EndpointConfigurationBuilder
    {
        public EndpointWithCriticalError()
        {
            EndpointSetup<DefaultServer>();
        }

        public class CriticalHandler : IHandleMessages<Message>
        {
            public CriticalHandler(CriticalError criticalError, TestContext testContext)
            {
                this.criticalError = criticalError;
                this.testContext = testContext;
            }

            public Task Handle(Message request, IMessageHandlerContext context)
            {
                if (testContext.ContextId == request.ContextId)
                {
                    criticalError.Raise("a critical error", new SimulatedException());
                    testContext.CriticalErrorsRaised++;
                }

                return Task.CompletedTask;
            }

            CriticalError criticalError;
            TestContext testContext;
        }
    }

    class CriticalErrorStartupFeatureTask : FeatureStartupTask
    {
        public CriticalErrorStartupFeatureTask(CriticalError criticalError, TestContext testContext)
        {
            this.criticalError = criticalError;
            this.testContext = testContext;
        }

        protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
        {
            criticalError.Raise("critical error 1", new SimulatedException(), cancellationToken);
            testContext.CriticalErrorsRaised++;

            criticalError.Raise("critical error 2", new SimulatedException(), cancellationToken);
            testContext.CriticalErrorsRaised++;

            return Task.CompletedTask;
        }

        protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        readonly TestContext testContext;

        CriticalError criticalError;
    }

    public class EndpointWithCriticalErrorStartup : EndpointConfigurationBuilder
    {
        public EndpointWithCriticalErrorStartup()
        {
            EndpointSetup<DefaultServer>(c => c.RegisterStartupTask<CriticalErrorStartupFeatureTask>());
        }
    }

    public class Message : IMessage
    {
        public string ContextId { get; set; }
    }
}