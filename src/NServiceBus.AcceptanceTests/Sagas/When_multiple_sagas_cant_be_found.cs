namespace NServiceBus.AcceptanceTests.Sagas;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_multiple_sagas_cant_be_found : NServiceBusAcceptanceTest
{
    [Test]
    public async Task NotFoundHandlers_called_on_all_not_found_sagas()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<ReceiverWithSagas>(b => b.When(session => session.SendLocal(new MessageToSaga { Id = Guid.NewGuid() })))
            .Done(c => c.Saga1NotFound && c.Saga2NotFound && c.Saga3Started)
            .Run();

        Assert.That(context.Saga1NotFound, Is.True);
        Assert.That(context.Saga2NotFound, Is.True);
        Assert.That(context.Saga3Started, Is.True);
        Assert.That(context.Saga3NotFound, Is.False);
    }

    public class Context : ScenarioContext
    {
        public bool Saga1NotFound { get; set; }
        public bool Saga2NotFound { get; set; }
        public bool Saga3Started { get; set; }
        public bool Saga3NotFound { get; set; }
    }

    public class ReceiverWithSagas : EndpointConfigurationBuilder
    {
        public ReceiverWithSagas() => EndpointSetup<DefaultServer>(c =>
        {
            c.AddSaga<CantBeFoundSaga1>();
            c.AddSaga<CantBeFoundSaga2>();
            c.AddSaga<FoundSaga>();
        });

        public class CantBeFoundSaga1 : Saga<CantBeFoundSaga1.CantBeFoundSaga1Data>, IAmStartedByMessages<StartSaga>, IHandleMessages<MessageToSaga>
        {
            public Task Handle(StartSaga message, IMessageHandlerContext context) => Task.CompletedTask;

            public Task Handle(MessageToSaga message, IMessageHandlerContext context) => Task.CompletedTask;

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<CantBeFoundSaga1Data> mapper)
            {
                mapper.MapSaga(s => s.MessageId)
                    .ToMessage<StartSaga>(m => m.Id)
                    .ToMessage<MessageToSaga>(m => m.Id);

                mapper.ConfigureNotFoundHandler<Saga1NotFound>();
            }

            public class CantBeFoundSaga1Data : ContainSagaData
            {
                public virtual Guid MessageId { get; set; }
            }

            public class Saga1NotFound(Context testContext) : ISagaNotFoundHandler
            {
                public Task Handle(object message, IMessageProcessingContext context)
                {
                    testContext.Saga1NotFound = true;
                    return Task.CompletedTask;
                }
            }
        }

        public class CantBeFoundSaga2 : Saga<CantBeFoundSaga2.CantBeFoundSaga2Data>, IAmStartedByMessages<StartSaga>, IHandleMessages<MessageToSaga>
        {
            public Task Handle(StartSaga message, IMessageHandlerContext context) => Task.CompletedTask;

            public Task Handle(MessageToSaga message, IMessageHandlerContext context) => Task.CompletedTask;

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<CantBeFoundSaga2Data> mapper)
            {
                mapper.MapSaga(s => s.MessageId)
                    .ToMessage<StartSaga>(m => m.Id)
                    .ToMessage<MessageToSaga>(m => m.Id);

                mapper.ConfigureNotFoundHandler<Saga2NotFound>();
            }

            public class CantBeFoundSaga2Data : ContainSagaData
            {
                public virtual Guid MessageId { get; set; }
            }

            public class Saga2NotFound(Context testContext) : ISagaNotFoundHandler
            {
                public Task Handle(object message, IMessageProcessingContext context)
                {
                    testContext.Saga2NotFound = true;
                    return Task.CompletedTask;
                }
            }
        }
    }

    public class FoundSaga(Context testContext) : Saga<FoundSaga.FoundSagaData>, IAmStartedByMessages<MessageToSaga>
    {
        public Task Handle(MessageToSaga message, IMessageHandlerContext context)
        {
            testContext.Saga3Started = true;
            return Task.CompletedTask;
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<FoundSagaData> mapper)
        {
            mapper.MapSaga(s => s.MessageId)
                .ToMessage<MessageToSaga>(m => m.Id);

            mapper.ConfigureNotFoundHandler<FoundSagaNotFoundHandler>();
        }

        public class FoundSagaData : ContainSagaData
        {
            public virtual Guid MessageId { get; set; }
        }

        public class FoundSagaNotFoundHandler(Context testContext) : ISagaNotFoundHandler
        {
            public Task Handle(object message, IMessageProcessingContext context)
            {
                testContext.Saga3NotFound = true;
                return Task.CompletedTask;
            }
        }
    }

    public class StartSaga : ICommand
    {
        public Guid Id { get; set; }
    }

    public class MessageToSaga : ICommand
    {
        public Guid Id { get; set; }
    }
}