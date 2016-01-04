namespace NServiceBus.AcceptanceTests.CriticalError
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;
    using CriticalError = NServiceBus.CriticalError;

    public class When_using_default_critical_error_handler : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_stop_endpoint_when_endpoint_started()
        {
            var context = await Scenario.Define<TestContext>()
                .WithEndpoint<EndpointWithCriticalError>(b => b
                    .When((bus, c) =>
                    {
                        c.ContextId = Guid.NewGuid().ToString();
                        return bus.SendLocal(new Message { ContextId = c.ContextId });
                    }))
                .Done(c => c.CriticalErrorRaised)
                .Run();

            Assert.IsNull(context.RaiseCriticalErrorException);
            Assert.IsTrue(context.CriticalErrorRaised);
        }

        [Test]
        public async Task Should_throw_when_endpoint_not_started()
        {
            var context = await Scenario.Define<TestContext>()
                .WithEndpoint<EndpointWithCriticalErrorStartup>()
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.IsTrue(context.CriticalErrorRaised);
            Assert.IsAssignableFrom<InvalidOperationException>(context.RaiseCriticalErrorException);
        }

        public class TestContext : ScenarioContext
        {
            public string ContextId { get; set; }
            public bool CriticalErrorRaised { get; set; }
            public Exception RaiseCriticalErrorException { get; set; }
        }

        public class EndpointWithCriticalError : EndpointConfigurationBuilder
        {
            public EndpointWithCriticalError()
            {
                EndpointSetup<DefaultServer>();
            }

            public class CriticalHandler : IHandleMessages<Message>
            {
                CriticalError criticalError;
                TestContext testContext;

                public CriticalHandler(CriticalError criticalError, TestContext testContext)
                {
                    this.criticalError = criticalError;
                    this.testContext = testContext;
                }

                public Task Handle(Message request, IMessageHandlerContext context)
                {
                    try
                    {
                        if (testContext.ContextId == request.ContextId)
                        {
                            criticalError.Raise("a ciritical error", new SimulatedException());
                        }
                    }
                    catch (Exception exception)
                    {
                        testContext.RaiseCriticalErrorException = exception;
                    }

                    if (testContext.ContextId == request.ContextId)
                    {
                        testContext.CriticalErrorRaised = true;
                    }

                    return Task.FromResult(0);
                }
            }
        }

        public class EndpointWithCriticalErrorStartup : EndpointConfigurationBuilder
        {
            public EndpointWithCriticalErrorStartup()
            {
                EndpointSetup<DefaultServer>();
            }

            public class CriticalErrorStartup : IWantToRunWhenBusStartsAndStops
            {
                CriticalError criticalError;
                TestContext testContext;

                public CriticalErrorStartup(CriticalError criticalError, TestContext testContext)
                {
                    this.criticalError = criticalError;
                    this.testContext = testContext;
                }

                public Task Start(IBusSession session)
                {
                    try
                    {
                        criticalError.Raise("critical error", new SimulatedException());
                    }
                    catch (Exception e)
                    {
                        testContext.RaiseCriticalErrorException = e;
                    }

                    testContext.CriticalErrorRaised = true;

                    return Task.FromResult(0);
                }

                public Task Stop(IBusSession session)
                {
                    return Task.FromResult(0);
                }
            }
        }

        [Serializable]
        public class Message : IMessage
        {
            public string ContextId { get; set; }
        }
    }
}