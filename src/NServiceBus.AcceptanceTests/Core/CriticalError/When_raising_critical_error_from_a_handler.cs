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

public class When_raising_critical_error_from_a_handler : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_trigger_critical_error_action()
    {
        var exceptions = new ConcurrentDictionary<string, Exception>();

        Func<ICriticalErrorContext, CancellationToken, Task> addCritical = (criticalContext, _) =>
        {
            exceptions.TryAdd(criticalContext.Error, criticalContext.Exception);
            return Task.CompletedTask;
        };

        await Scenario.Define<TestContext>()
            .WithEndpoint<EndpointWithCriticalError>(b =>
            {
                b.CustomConfig(config => { config.DefineCriticalErrorAction(addCritical); });

                b.When((session, c) =>
                {
                    c.ContextId = Guid.NewGuid().ToString();
                    return session.SendLocal(new Message
                    {
                        ContextId = c.ContextId
                    });
                });
            })
            .Done(c => c.CriticalErrorsRaised > 0 && exceptions.Keys.Count > 0)
            .Run();

        Assert.That(exceptions.Keys, Has.Count.EqualTo(1));
    }

    public class TestContext : ScenarioContext
    {
        public string ContextId { get; set; }
        public int CriticalErrorsRaised { get; set; }
    }

    public class EndpointWithCriticalError : EndpointConfigurationBuilder
    {
        public EndpointWithCriticalError() => EndpointSetup<DefaultServer>();

        public class CriticalHandler(CriticalError criticalError, TestContext testContext) : IHandleMessages<Message>
        {
            public Task Handle(Message request, IMessageHandlerContext context)
            {
                if (testContext.ContextId == request.ContextId)
                {
                    criticalError.Raise("a critical error", new SimulatedException());
                    testContext.CriticalErrorsRaised++;
                }

                return Task.CompletedTask;
            }
        }
    }
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

    public class EndpointWithCriticalErrorStartup : EndpointConfigurationBuilder
    {
        public EndpointWithCriticalErrorStartup() =>
            EndpointSetup<DefaultServer>(c => c
                .RegisterStartupTask<CriticalErrorStartupFeatureTask>());
    }

    public class Message : IMessage
    {
        public string ContextId { get; set; }
    }
}