﻿namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Features;
    using NServiceBus.Sagas;
    using NUnit.Framework;

    public class when_receiving_multiple_timeouts : NServiceBusAcceptanceTest
    {
        // realted to NSB issue #1819
        [Test]
        public async Task It_should_not_invoke_SagaNotFound_handler()
        {
            var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                    .WithEndpoint<Endpoint>(b => b.When((bus, c) => bus.SendLocalAsync(new StartSaga1 { ContextId = c.Id })))
                    .Done(c => (c.Saga1TimeoutFired && c.Saga2TimeoutFired) || c.SagaNotFound)
                    .Run(TimeSpan.FromSeconds(20));

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
                });
            }

            public class MultTimeoutsSaga1 : Saga<MultTimeoutsSaga1.MultTimeoutsSaga1Data>, 
                IAmStartedByMessages<StartSaga1>, 
                IHandleTimeouts<Saga1Timeout>, 
                IHandleTimeouts<Saga2Timeout>
            {
                public Context TestContext { get; set; }

                public async Task Handle(StartSaga1 message, IMessageHandlerContext context)
                {
                    if (message.ContextId != TestContext.Id)
                        return;

                    context.GetSagaData<MultTimeoutsSaga1Data>().ContextId = message.ContextId;

                    await context.RequestTimeoutAsync(TimeSpan.FromSeconds(5), new Saga1Timeout { ContextId = TestContext.Id });
                    await context.RequestTimeoutAsync(TimeSpan.FromMilliseconds(10), new Saga2Timeout { ContextId = TestContext.Id });
                }

                public Task Timeout(Saga1Timeout state, IMessageHandlerContext context)
                {
                    context.MarkAsComplete();

                    if (state.ContextId == TestContext.Id)
                    {
                        TestContext.Saga1TimeoutFired = true;
                    }

                    return Task.FromResult(0);
                }

                public Task Timeout(Saga2Timeout state, IMessageHandlerContext context)
                {
                    if (state.ContextId == TestContext.Id)
                    {
                        TestContext.Saga2TimeoutFired = true;
                    }

                    return Task.FromResult(0);
                }

                public class MultTimeoutsSaga1Data : ContainSagaData
                {
                    public virtual Guid ContextId { get; set; }
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MultTimeoutsSaga1Data> mapper)
                {
                    mapper.ConfigureMapping<StartSaga1>(m => m.ContextId)
                        .ToSaga(s => s.Id);
                }
            }

            public class SagaNotFound : IHandleSagaNotFound
            {
                public Context Context { get; set; }

                public Task Handle(object message, IMessageHandlerContext context)
                {
                    if (((dynamic)message).ContextId != Context.Id) return Task.FromResult(0);

                    Context.SagaNotFound = true;

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

        [Serializable]
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