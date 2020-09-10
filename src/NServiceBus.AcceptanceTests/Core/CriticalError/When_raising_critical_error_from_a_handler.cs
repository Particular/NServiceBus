namespace NServiceBus.AcceptanceTests.Core.CriticalError
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using CriticalError = NServiceBus.CriticalError;

    public class When_raising_critical_error_from_a_handler : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_trigger_critical_error_action()
        {
            var exceptions = new ConcurrentDictionary<string, Exception>();

            Func<ICriticalErrorContext, Task> addCritical = criticalContext =>
            {
                exceptions.TryAdd(criticalContext.Error, criticalContext.Exception);
                return Task.FromResult(0);
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

            Assert.AreEqual(1, exceptions.Keys.Count);
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

                public Task Handle(Message request, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
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

                protected override Task OnStart(IMessageSession session)
                {
                    criticalError.Raise("critical error 1", new SimulatedException());
                    testContext.CriticalErrorsRaised++;

                    criticalError.Raise("critical error 2", new SimulatedException());
                    testContext.CriticalErrorsRaised++;

                    return Task.FromResult(0);
                }

                protected override Task OnStop(IMessageSession session)
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