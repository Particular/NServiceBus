namespace NServiceBus.AcceptanceTests.Core.CriticalError
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using Microsoft.Extensions.DependencyInjection;
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
                return Task.FromResult(0);
            };

            var context = await Scenario.Define<TestContext>()
                .WithEndpoint<EndpointWithCriticalErrorStartup>(b =>
                    b.CustomConfig(config => config.DefineCriticalErrorAction(addCritical)))
                .Done(c => c.CriticalErrorsRaised >= 2 && exceptions.Count >= 2)
                .Run();

            Assert.AreEqual(2, context.CriticalErrorsRaised);
            Assert.AreEqual(context.CriticalErrorsRaised, exceptions.Count);
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

                    return Task.FromResult(0);
                }

                CriticalError criticalError;
                TestContext testContext;
            }
        }

        class CriticalErrorStartup : Feature
        {
            protected override void Setup(FeatureConfigurationContext context)
            {
                context.RegisterStartupTask(b => new CriticalErrorStartupFeatureTask(b.GetService<CriticalError>(), b.GetService<TestContext>()));
            }

            class CriticalErrorStartupFeatureTask : FeatureStartupTask
            {
                public CriticalErrorStartupFeatureTask(CriticalError criticalError, TestContext testContext)
                {
                    this.criticalError = criticalError;
                    this.testContext = testContext;
                }

                protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken)
                {
                    criticalError.Raise("critical error 1", new SimulatedException(), cancellationToken);
                    testContext.CriticalErrorsRaised++;

                    criticalError.Raise("critical error 2", new SimulatedException(), cancellationToken);
                    testContext.CriticalErrorsRaised++;

                    return Task.FromResult(0);
                }

                protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken)
                {
                    return Task.FromResult(0);
                }

                readonly TestContext testContext;

                CriticalError criticalError;
            }
        }

        public class EndpointWithCriticalErrorStartup : EndpointConfigurationBuilder
        {
            public EndpointWithCriticalErrorStartup()
            {
                EndpointSetup<DefaultServer>(c => c.EnableFeature<CriticalErrorStartup>());
            }
        }

        public class Message : IMessage
        {
            public string ContextId { get; set; }
        }
    }
}