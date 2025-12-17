namespace NServiceBus.AcceptanceTests.Core.CriticalError;

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;
using CriticalError = NServiceBus.CriticalError;

public class When_raising_critical_error_from_a_handler : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_trigger_critical_error_action()
    {
        var exceptions = new ConcurrentDictionary<string, Exception>();

        Task CollectCriticalErrors(ICriticalErrorContext criticalContext, CancellationToken _)
        {
            exceptions.TryAdd(criticalContext.Error, criticalContext.Exception);
            return Task.CompletedTask;
        }

        await Scenario.Define<TestContext>()
            .WithEndpoint<EndpointWithCriticalError>(b =>
            {
                b.CustomConfig(config => config.DefineCriticalErrorAction(CollectCriticalErrors));
                b.When((session, c) =>
                {
                    c.ContextId = Guid.NewGuid().ToString();
                    return session.SendLocal(new Message
                    {
                        ContextId = c.ContextId
                    });
                });
            })
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
                    testContext.MarkAsCompleted();
                }

                return Task.CompletedTask;
            }
        }
    }

    public class Message : IMessage
    {
        public string ContextId { get; set; }
    }
}