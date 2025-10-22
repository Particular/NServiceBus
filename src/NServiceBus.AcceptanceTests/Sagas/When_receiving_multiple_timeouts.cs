namespace NServiceBus.AcceptanceTests.Sagas;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_receiving_multiple_timeouts : NServiceBusAcceptanceTest
{
    // related to NSB issue #1819
    [Test]
    public async Task It_should_not_invoke_SagaNotFound_handler()
    {
        Requires.DelayedDelivery();

        var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
            .WithEndpoint<Endpoint>(b => b.When((session, c) => session.SendLocal(new StartSaga1 { ContextId = c.Id })))
            .Done(c => (c.Saga1TimeoutFired && c.Saga2TimeoutFired) || c.SagaNotFound)
            .Run(TimeSpan.FromSeconds(60));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.SagaNotFound, Is.False);
            Assert.That(context.Saga1TimeoutFired, Is.True);
            Assert.That(context.Saga2TimeoutFired, Is.True);
        }
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
        public Endpoint() =>
            EndpointSetup<DefaultServer>(c =>
            {
                c.ExecuteTheseHandlersFirst(typeof(CatchAllMessageHandler));
                c.Recoverability().Immediate(immediate => immediate.NumberOfRetries(5));
            });

        public class MultiTimeoutsSaga1(Context testContext) : Saga<MultiTimeoutsSaga1.MultiTimeoutsSaga1Data>,
            IAmStartedByMessages<StartSaga1>,
            IHandleTimeouts<Saga1Timeout>,
            IHandleTimeouts<Saga2Timeout>
        {
            public async Task Handle(StartSaga1 message, IMessageHandlerContext context)
            {
                await RequestTimeout<Saga1Timeout>(context, TimeSpan.FromMilliseconds(1));
                await RequestTimeout<Saga2Timeout>(context, TimeSpan.FromMilliseconds(1));
            }

            public Task Timeout(Saga1Timeout state, IMessageHandlerContext context)
            {
                if (testContext.Saga1TimeoutFired && testContext.Saga2TimeoutFired)
                {
                    MarkAsComplete();
                }

                return Task.CompletedTask;
            }

            public Task Timeout(Saga2Timeout state, IMessageHandlerContext context)
            {
                if (testContext.Saga1TimeoutFired && testContext.Saga2TimeoutFired)
                {
                    MarkAsComplete();
                }

                return Task.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MultiTimeoutsSaga1Data> mapper)
            {
                mapper.ConfigureMapping<StartSaga1>(m => m.ContextId)
                    .ToSaga(s => s.ContextId);
                mapper.ConfigureNotFoundHandler<SagaNotFound>();
            }

            public class MultiTimeoutsSaga1Data : ContainSagaData
            {
                public virtual Guid ContextId { get; set; }
            }
        }

        public class SagaNotFound(Context testContext) : ISagaNotFoundHandler
        {
            public Task Handle(object message, IMessageProcessingContext context)
            {
                testContext.SagaNotFound = true;

                return Task.CompletedTask;
            }
        }

        public class CatchAllMessageHandler : IHandleMessages<object>
        {
            public Task Handle(object message, IMessageHandlerContext context) => Task.CompletedTask;
        }
    }


    public class StartSaga1 : ICommand
    {
        public Guid ContextId { get; set; }
    }

    public class Saga1Timeout;

    public class Saga2Timeout;
}