namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Features;
    using NUnit.Framework;
    using Saga;

    public class When_sagas_cant_be_found : NServiceBusAcceptanceTest
    {
        [Test]
        public void IHandleSagaNotFound_only_called_once()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<ReceiverWithSagas>(b => b.Given((bus, c) => bus.SendLocal(new MessageToSaga { Id = Guid.NewGuid() })))
                    .Done(c => c.Done)
                    .Run();

            Assert.AreEqual(1, context.TimesFired);
        }

        [Test]
        public void IHandleSagaNotFound_not_called_if_second_saga_is_executed()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<ReceiverWithOrderedSagas>(b => b.Given((bus, c) => bus.SendLocal(new MessageToSaga { Id = Guid.NewGuid() })))
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
                public IBus Bus { get; set; }

                public void Handle(MessageToSaga message)
                {
                    var options = new SendOptions();

                    options.DelayDeliveryWith(TimeSpan.FromSeconds(10));
                    options.RouteToLocalEndpointInstance();

                    Bus.Send(new FinishMessage(), options);
                }
            }

            public class FinishHandler : IHandleMessages<FinishMessage>
            {
                public Context Context { get; set; }

                public void Handle(FinishMessage message)
                {
                    Context.Done = true;
                }
            }

            public class CantBeFoundSaga1 : Saga<CantBeFoundSaga1.CantBeFoundSaga1Data>, IAmStartedByMessages<StartSaga>, IHandleMessages<MessageToSaga>
            {

                public void Handle(StartSaga message)
                {
                    Data.MessageId = message.Id;
                }

                public void Handle(MessageToSaga message)
                {
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

                public void Handle(StartSaga message)
                {
                    Data.MessageId = message.Id;
                }

                public void Handle(MessageToSaga message)
                {
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
                public Context Context { get; set; }

                public void Handle(object message)
                {
                    Context.TimesFired++;
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
                public IBus Bus { get; set; }

                public void Handle(MessageToSaga message)
                {
                    var options = new SendOptions();

                    options.DelayDeliveryWith(TimeSpan.FromSeconds(10));
                    options.RouteToLocalEndpointInstance();

                    Bus.Send(new FinishMessage(), options);
                }
            }

            public class FinishHandler : IHandleMessages<FinishMessage>
            {
                public Context Context { get; set; }

                public void Handle(FinishMessage message)
                {
                    Context.Done = true;
                }
            }

            public class ReceiverWithOrderedSagasSaga1 : Saga<ReceiverWithOrderedSagasSaga1.ReceiverWithOrderedSagasSaga1Data>, IAmStartedByMessages<StartSaga>, IHandleMessages<MessageToSaga>
            {
                public void Handle(StartSaga message)
                {
                    Data.MessageId = message.Id;
                }

                public void Handle(MessageToSaga message)
                {
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
                public void Handle(StartSaga message)
                {
                    Data.MessageId = message.Id;
                }

                public void Handle(MessageToSaga message)
                {
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
                public Context Context { get; set; }

                public void Handle(object message)
                {
                    Context.TimesFired++;
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