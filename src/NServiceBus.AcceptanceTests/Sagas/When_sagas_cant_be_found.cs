namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Features;
    using NServiceBus.Sagas;
    using NUnit.Framework;

    public class When_sagas_cant_be_found : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task IHandleSagaNotFound_only_called_once()
        {
            var context = await Scenario.Define<Context>()
                    .WithEndpoint<ReceiverWithSagas>(b => b.When((bus, c) => bus.SendLocal(new MessageToSaga { Id = Guid.NewGuid() })))
                    .Done(c => c.Done)
                    .Run();

            Assert.AreEqual(1, context.TimesFired);
        }

        [Test]
        public async Task IHandleSagaNotFound_not_called_if_second_saga_is_executed()
        {
            var context = await Scenario.Define<Context>()
                     .WithEndpoint<ReceiverWithOrderedSagas>(b => b.When((bus, c) => bus.SendLocal(new MessageToSaga { Id = Guid.NewGuid() })))
                     .Done(c => c.Done)
                     .Run();

            Assert.AreEqual(0, context.TimesFired);
        }

        public class Context : ScenarioContext
        {
            public int TimesFired { get; set; }
            public bool Done { get; set; }
        }

        public class ReceiverWithSagas : EndpointConfigurationBuilder
        {
            public ReceiverWithSagas()
            {
                EndpointSetup<DefaultServer>(config => config.EnableFeature<TimeoutManager>());
            }

            public class MessageToSagaHandler : IHandleMessages<MessageToSaga>
            {
                public Task Handle(MessageToSaga message, IMessageHandlerContext context)
                {
                    var options = new SendOptions();

                    options.DelayDeliveryWith(TimeSpan.FromMilliseconds(1));
                    options.RouteToLocalEndpointInstance();

                    return context.Send(new FinishMessage(), options);
                }
            }

            public class FinishHandler : IHandleMessages<FinishMessage>
            {
                public Context Context { get; set; }

                public Task Handle(FinishMessage message, IMessageHandlerContext context)
                {
                    // This acts as a safe guard to abort the test earlier
                    Context.Done = true;
                    return Task.FromResult(0);
                }
            }

            public class CantBeFoundSaga1 : Saga<CantBeFoundSaga1.CantBeFoundSaga1Data>, IAmStartedByMessages<StartSaga>, IHandleMessages<MessageToSaga>
            {

                public Task Handle(StartSaga message, IMessageHandlerContext context)
                {
                    Data.MessageId = message.Id;
                    return Task.FromResult(0);
                }

                public Task Handle(MessageToSaga message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }

                public class CantBeFoundSaga1Data : ContainSagaData
                {
                    public virtual Guid MessageId { get; set; }
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<CantBeFoundSaga1Data> mapper)
                {
                    mapper.ConfigureMapping<StartSaga>(m => m.Id).ToSaga(s => s.MessageId);
                    mapper.ConfigureMapping<MessageToSaga>(m => m.Id).ToSaga(s => s.MessageId);
                }
            }

            public class CantBeFoundSaga2 : Saga<CantBeFoundSaga2.CantBeFoundSaga2Data>, IAmStartedByMessages<StartSaga>, IHandleMessages<MessageToSaga>
            {

                public Task Handle(StartSaga message, IMessageHandlerContext context)
                {
                    Data.MessageId = message.Id;
                    return Task.FromResult(0);
                }

                public Task Handle(MessageToSaga message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }

                public class CantBeFoundSaga2Data : ContainSagaData
                {
                    public virtual Guid MessageId { get; set; }
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<CantBeFoundSaga2Data> mapper)
                {
                    mapper.ConfigureMapping<StartSaga>(m => m.Id).ToSaga(s => s.MessageId);
                    mapper.ConfigureMapping<MessageToSaga>(m => m.Id).ToSaga(s => s.MessageId);
                }
            }

            public class SagaNotFound : IHandleSagaNotFound
            {
                public Context TestContext { get; set; }

                public Task Handle(object message, IMessageProcessingContext context)
                {
                    TestContext.TimesFired++;
                    TestContext.Done = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class ReceiverWithOrderedSagas : EndpointConfigurationBuilder
        {
            public ReceiverWithOrderedSagas()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.EnableFeature<TimeoutManager>();
                    c.ExecuteTheseHandlersFirst(typeof(ReceiverWithOrderedSagasSaga1), typeof(ReceiverWithOrderedSagasSaga2));
                });
            }

            public class MessageToSagaHandler : IHandleMessages<MessageToSaga>
            {
                public Task Handle(MessageToSaga message, IMessageHandlerContext context)
                {
                    var options = new SendOptions();

                    options.DelayDeliveryWith(TimeSpan.FromMilliseconds(1));
                    options.RouteToLocalEndpointInstance();

                    return context.Send(new FinishMessage(), options);
                }
            }

            public class FinishHandler : IHandleMessages<FinishMessage>
            {
                public Context Context { get; set; }

                public Task Handle(FinishMessage message, IMessageHandlerContext context)
                {
                    // This acts as a safe guard to abort the test earlier
                    Context.Done = true;
                    return Task.FromResult(0);
                }
            }

            public class ReceiverWithOrderedSagasSaga1 : Saga<ReceiverWithOrderedSagasSaga1.ReceiverWithOrderedSagasSaga1Data>, IAmStartedByMessages<StartSaga>, IHandleMessages<MessageToSaga>
            {
                public Task Handle(StartSaga message, IMessageHandlerContext context)
                {
                    Data.MessageId = message.Id;
                    return Task.FromResult(0);
                }

                public Task Handle(MessageToSaga message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }

                public class ReceiverWithOrderedSagasSaga1Data : ContainSagaData
                {
                    public virtual Guid MessageId { get; set; }
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<ReceiverWithOrderedSagasSaga1Data> mapper)
                {
                    mapper.ConfigureMapping<StartSaga>(m => m.Id).ToSaga(s => s.MessageId);
                    mapper.ConfigureMapping<MessageToSaga>(m => m.Id).ToSaga(s => s.MessageId);
                }
            }

            public class ReceiverWithOrderedSagasSaga2 : Saga<ReceiverWithOrderedSagasSaga2.ReceiverWithOrderedSagasSaga2Data>, IHandleMessages<StartSaga>, IAmStartedByMessages<MessageToSaga>
            {
                public Context Context { get; set; }

                public Task Handle(StartSaga message, IMessageHandlerContext context)
                {
                    Data.MessageId = message.Id;
                    return Task.FromResult(0);
                }

                public Task Handle(MessageToSaga message, IMessageHandlerContext context)
                {
                    Data.MessageId = message.Id;
                    Context.Done = true;
                    return Task.FromResult(0);
                }

                public class ReceiverWithOrderedSagasSaga2Data : ContainSagaData
                {
                    public virtual Guid MessageId { get; set; }
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<ReceiverWithOrderedSagasSaga2Data> mapper)
                {
                    mapper.ConfigureMapping<StartSaga>(m => m.Id).ToSaga(s => s.MessageId);
                    mapper.ConfigureMapping<MessageToSaga>(m => m.Id).ToSaga(s => s.MessageId);
                }
            }

            public class SagaNotFound : IHandleSagaNotFound
            {
                public Context TestContext { get; set; }

                public Task Handle(object message, IMessageProcessingContext context)
                {
                    TestContext.TimesFired++;
                    return Task.FromResult(0);
                }
            }
        }

        [Serializable]
        public class StartSaga : ICommand
        {
            public Guid Id { get; set; }
        }

        [Serializable]
        public class FinishMessage : ICommand
        {
        }

        [Serializable]
        public class MessageToSaga : ICommand
        {
            public Guid Id { get; set; }
        }
    }
}