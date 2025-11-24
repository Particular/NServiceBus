namespace NServiceBus.AcceptanceTests.Sagas;

using System;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NServiceBus.Sagas;
using NUnit.Framework;

public class When_sagas_cant_be_found : NServiceBusAcceptanceTest
{
    [Test]
    public async Task IHandleSagaNotFound_only_called_once()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<ReceiverWithSagas>(b => b.When((session, c) => session.SendLocal(new MessageToSaga
            {
                Id = Guid.NewGuid()
            })))
            .Done(c => c.Done)
            .Run();

        Assert.That(context.TimesFired, Is.EqualTo(1));
    }

    [Test]
    public async Task IHandleSagaNotFound_not_called_if_second_saga_is_executed()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<ReceiverWithOrderedSagas>(b => b.When((session, c) => session.SendLocal(new MessageToSaga
            {
                Id = Guid.NewGuid()
            })))
            .Done(c => c.Done)
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Logs.Any(m => m.Message.Equals("Could not find a started saga of 'NServiceBus.AcceptanceTests.Sagas.When_sagas_cant_be_found+ReceiverWithOrderedSagas+ReceiverWithOrderedSagasSaga1' for message type 'NServiceBus.AcceptanceTests.Sagas.When_sagas_cant_be_found+MessageToSaga'.")), Is.True);
            Assert.That(context.Logs.Any(m => m.Message.Equals("Could not find a started saga of 'NServiceBus.AcceptanceTests.Sagas.When_sagas_cant_be_found+ReceiverWithOrderedSagas+ReceiverWithOrderedSagasSaga2' for message type 'NServiceBus.AcceptanceTests.Sagas.When_sagas_cant_be_found+MessageToSaga'.")), Is.False);
            Assert.That(context.Logs.Any(m => m.Message.Contains("Going to invoke SagaNotFoundHandlers.")), Is.False);

            Assert.That(context.TimesFired, Is.EqualTo(0));
        }
    }

    public class Context : ScenarioContext
    {
        public int TimesFired { get; set; }
        public bool Done { get; set; }
    }

    public class ReceiverWithSagas : EndpointConfigurationBuilder
    {
        public ReceiverWithSagas() => EndpointSetup<DefaultServer>();

        public class CantBeFoundSaga1 : Saga<CantBeFoundSaga1.CantBeFoundSaga1Data>, IAmStartedByMessages<StartSaga>, IHandleMessages<MessageToSaga>
        {
            public Task Handle(StartSaga message, IMessageHandlerContext context)
            {
                Data.MessageId = message.Id;
                return Task.CompletedTask;
            }

            public Task Handle(MessageToSaga message, IMessageHandlerContext context) => Task.CompletedTask;

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<CantBeFoundSaga1Data> mapper) =>
                mapper.MapSaga(s => s.MessageId)
                    .ToMessage<StartSaga>(m => m.Id)
                    .ToMessage<MessageToSaga>(m => m.Id);

            public class CantBeFoundSaga1Data : ContainSagaData
            {
                public virtual Guid MessageId { get; set; }
            }
        }

        public class CantBeFoundSaga2 : Saga<CantBeFoundSaga2.CantBeFoundSaga2Data>, IAmStartedByMessages<StartSaga>, IHandleMessages<MessageToSaga>
        {
            public Task Handle(StartSaga message, IMessageHandlerContext context)
            {
                Data.MessageId = message.Id;
                return Task.CompletedTask;
            }

            public Task Handle(MessageToSaga message, IMessageHandlerContext context) => Task.CompletedTask;

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<CantBeFoundSaga2Data> mapper) =>
                mapper.MapSaga(s => s.MessageId)
                    .ToMessage<StartSaga>(m => m.Id)
                    .ToMessage<MessageToSaga>(m => m.Id);

            public class CantBeFoundSaga2Data : ContainSagaData
            {
                public virtual Guid MessageId { get; set; }
            }
        }

        public class SagaNotFound(Context testContext) : IHandleSagaNotFound
        {
            public Task Handle(object message, IMessageProcessingContext context)
            {
                testContext.TimesFired++;
                testContext.Done = true;
                return Task.CompletedTask;
            }
        }
    }

    public class ReceiverWithOrderedSagas : EndpointConfigurationBuilder
    {
        public ReceiverWithOrderedSagas() =>
            EndpointSetup<DefaultServer>(c =>
            {
                c.AddHandler<ReceiverWithOrderedSagasSaga1>();
                c.AddHandler<ReceiverWithOrderedSagasSaga2>();
            });

        public class ReceiverWithOrderedSagasSaga1 : Saga<ReceiverWithOrderedSagasSaga1.ReceiverWithOrderedSagasSaga1Data>, IAmStartedByMessages<StartSaga>, IHandleMessages<MessageToSaga>
        {
            public Task Handle(StartSaga message, IMessageHandlerContext context)
            {
                Data.MessageId = message.Id;
                return Task.CompletedTask;
            }

            public Task Handle(MessageToSaga message, IMessageHandlerContext context) => Task.CompletedTask;

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<ReceiverWithOrderedSagasSaga1Data> mapper) =>
                mapper.MapSaga(s => s.MessageId)
                    .ToMessage<StartSaga>(m => m.Id)
                    .ToMessage<MessageToSaga>(m => m.Id);

            public class ReceiverWithOrderedSagasSaga1Data : ContainSagaData
            {
                public virtual Guid MessageId { get; set; }
            }
        }

        public class ReceiverWithOrderedSagasSaga2(Context context) : Saga<ReceiverWithOrderedSagasSaga2.ReceiverWithOrderedSagasSaga2Data>, IHandleMessages<StartSaga>, IAmStartedByMessages<MessageToSaga>
        {
            public Task Handle(MessageToSaga message, IMessageHandlerContext context1)
            {
                Data.MessageId = message.Id;
                context.Done = true;
                return Task.CompletedTask;
            }

            public Task Handle(StartSaga message, IMessageHandlerContext context)
            {
                Data.MessageId = message.Id;
                return Task.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<ReceiverWithOrderedSagasSaga2Data> mapper) =>
                mapper.MapSaga(s => s.MessageId)
                    .ToMessage<StartSaga>(m => m.Id)
                    .ToMessage<MessageToSaga>(m => m.Id);

            public class ReceiverWithOrderedSagasSaga2Data : ContainSagaData
            {
                public virtual Guid MessageId { get; set; }
            }
        }

        public class SagaNotFound(Context testContext) : IHandleSagaNotFound
        {
            public Task Handle(object message, IMessageProcessingContext context)
            {
                testContext.TimesFired++;
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