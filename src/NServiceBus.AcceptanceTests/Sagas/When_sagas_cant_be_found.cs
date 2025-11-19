namespace NServiceBus.AcceptanceTests.Sagas;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_sagas_cant_be_found : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Handler_called_for_each_saga()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<ReceiverWithSagas>(b => b.When((session, c) => session.SendLocal(new MessageToSaga { Id = Guid.NewGuid() })))
            .Done(c => c.Saga1NotFound && c.Saga2NotFound)
            .Run();

        Assert.That(context.Saga1NotFound, Is.True);
        Assert.That(context.Saga2NotFound, Is.True);
    }

    public class Context : ScenarioContext
    {
        public bool Saga1NotFound { get; set; }
        public bool Saga2NotFound { get; set; }
    }

    public class ReceiverWithSagas : EndpointConfigurationBuilder
    {
        public ReceiverWithSagas() => EndpointSetup<DefaultServer>();

        public class CantBeFoundSaga1 : Saga<CantBeFoundSaga1.CantBeFoundSaga1Data>, IAmStartedByMessages<StartSaga>, IHandleMessages<MessageToSaga>
        {
            public Task Handle(StartSaga message, IMessageHandlerContext context) => Task.CompletedTask;

            public Task Handle(MessageToSaga message, IMessageHandlerContext context) => Task.CompletedTask;

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<CantBeFoundSaga1Data> mapper)
            {
                mapper.ConfigureMapping<StartSaga>(m => m.Id).ToSaga(s => s.MessageId);
                mapper.ConfigureMapping<MessageToSaga>(m => m.Id).ToSaga(s => s.MessageId);
                mapper.ConfigureNotFoundHandler<SagaNotFound>();
            }

            public class CantBeFoundSaga1Data : ContainSagaData
            {
                public virtual Guid MessageId { get; set; }
            }

            public class SagaNotFound(Context testContext) : ISagaNotFoundHandler
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
                mapper.ConfigureMapping<StartSaga>(m => m.Id).ToSaga(s => s.MessageId);
                mapper.ConfigureMapping<MessageToSaga>(m => m.Id).ToSaga(s => s.MessageId);
                mapper.ConfigureNotFoundHandler<SagaNotFound>();
            }

            public class CantBeFoundSaga2Data : ContainSagaData
            {
                public virtual Guid MessageId { get; set; }
            }

            public class SagaNotFound(Context testContext) : ISagaNotFoundHandler
            {
                public Task Handle(object message, IMessageProcessingContext context)
                {
                    testContext.Saga2NotFound = true;
                    return Task.CompletedTask;
                }
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