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
        var context = await Scenario.Define<TestContext>()
            .WithEndpoint<EndpointWithCriticalError>(b =>
            {
                b.CustomConfig((config, ctx) => config.DefineCriticalErrorAction((errorContext, token) => CollectCriticalErrors(errorContext, ctx, token)));
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

        Assert.That(context.Exceptions.Keys, Has.Count.EqualTo(1));

        Task CollectCriticalErrors(ICriticalErrorContext criticalContext, TestContext testContext, CancellationToken cancellationToken)
        {
            testContext.Exceptions.TryAdd(criticalContext.Error, criticalContext.Exception);
            testContext.MarkAsCompleted(!testContext.Exceptions.IsEmpty);
            return Task.CompletedTask;
        }
    }

    public class TestContext : ScenarioContext
    {
        public string ContextId { get; set; }
        public int CriticalErrorsRaised { get; set; }
        public ConcurrentDictionary<string, Exception> Exceptions { get; } = [];
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

    public class Message : IMessage
    {
        public string ContextId { get; set; }
    }
}