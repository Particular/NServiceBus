namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NServiceBus.Sagas;
    using NUnit.Framework;

    public class When_receiving_multiple_timeouts : NServiceBusAcceptanceTest
    {
        // related to NSB issue #1819
        [Test]
        public async Task It_should_not_invoke_SagaNotFound_handler()
        {
            var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                .WithEndpoint<Endpoint>(b => b.When((session, c) => session.SendLocal(new StartSaga1
                {
                    ContextId = c.Id
                })))
                .Done(c => (c.Saga1TimeoutFired && c.Saga2TimeoutFired) || c.SagaNotFound)
                .Run(TimeSpan.FromSeconds(60));

            Assert.IsFalse(context.SagaNotFound);
            Assert.IsTrue(context.Saga1TimeoutFired);
            Assert.IsTrue(context.Saga2TimeoutFired);
        }

        public class Context : ScenarioContext
        {
            public Guid Id { get; set; }
            public bool Saga1TimeoutFired { get; set; }
            public bool Saga2TimeoutFired { get; set; }
            public bool SagaNotFound { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.EnableFeature<TimeoutManager>();
                    c.ExecuteTheseHandlersFirst(typeof(CatchAllMessageHandler));
                    c.Recoverability().Immediate(immediate => immediate.NumberOfRetries(5));
                });
            }

            public class MultiTimeoutsSaga1 : Saga<MultiTimeoutsSaga1.MultiTimeoutsSaga1Data>,
                IAmStartedByMessages<StartSaga1>,
                IHandleTimeouts<Saga1Timeout>,
                IHandleTimeouts<Saga2Timeout>
            {
                public MultiTimeoutsSaga1(Context context)
                {
                    testContext = context;
                }

                public async Task Handle(StartSaga1 message, IMessageHandlerContext context)
                {
                    if (message.ContextId != testContext.Id)
                    {
                        return;
                    }

                    Data.ContextId = message.ContextId;

                    await RequestTimeout(context, TimeSpan.FromMilliseconds(1), new Saga1Timeout
                    {
                        ContextId = testContext.Id
                    });
                    await RequestTimeout(context, TimeSpan.FromMilliseconds(1), new Saga2Timeout
                    {
                        ContextId = testContext.Id
                    });
                }

                public Task Timeout(Saga1Timeout state, IMessageHandlerContext context)
                {
                    if (state.ContextId == testContext.Id)
                    {
                        testContext.Saga1TimeoutFired = true;
                    }

                    if (testContext.Saga1TimeoutFired && testContext.Saga2TimeoutFired)
                    {
                        MarkAsComplete();
                    }
                    return Task.FromResult(0);
                }

                public Task Timeout(Saga2Timeout state, IMessageHandlerContext context)
                {
                    if (state.ContextId == testContext.Id)
                    {
                        testContext.Saga2TimeoutFired = true;
                    }

                    if (testContext.Saga1TimeoutFired && testContext.Saga2TimeoutFired)
                    {
                        MarkAsComplete();
                    }
                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MultiTimeoutsSaga1Data> mapper)
                {
                    mapper.ConfigureMapping<StartSaga1>(m => m.ContextId)
                        .ToSaga(s => s.ContextId);
                }

                public class MultiTimeoutsSaga1Data : ContainSagaData
                {
                    public virtual Guid ContextId { get; set; }
                }

                Context testContext;
            }

            public class SagaNotFound : IHandleSagaNotFound
            {
                public Context TestContext { get; set; }

                public Task Handle(object message, IMessageProcessingContext context)
                {
                    if (((dynamic) message).ContextId != TestContext.Id)
                    {
                        return Task.FromResult(0);
                    }

                    TestContext.SagaNotFound = true;

                    return Task.FromResult(0);
                }
            }

            public class CatchAllMessageHandler : IHandleMessages<object>
            {
                public Task Handle(object message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }
            }
        }


        public class StartSaga1 : ICommand
        {
            public Guid ContextId { get; set; }
        }

        public class Saga1Timeout
        {
            public Guid ContextId { get; set; }
        }

        public class Saga2Timeout
        {
            public Guid ContextId { get; set; }
        }
    }
}
