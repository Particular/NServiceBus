namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using Saga;

    public class When_sagas_cant_be_found : NServiceBusAcceptanceTest
    {
        [Test]
        public void IHandleSagaNotFound_only_called_once()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<ReceiverWithSagas>(b => b.Given((bus, c) => bus.SendLocal(new MessageToSaga())))
                    .Done(c => c.Done)
                    .Run();

            Assert.AreEqual(1, context.TimesFired);
        }

        [Test]
        public void IHandleSagaNotFound_not_called_if_second_saga_is_executed()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<ReceiverWithOrderedSagas>(b => b.Given((bus, c) => bus.SendLocal(new MessageToSaga())))
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
                EndpointSetup<DefaultServer>();
            }

            public class MessageToSagaHandler : IHandleMessages<MessageToSaga>
            {
                public IBus Bus { get; set; }

                public void Handle(MessageToSaga message)
                {
                    Bus.Defer(TimeSpan.FromSeconds(10), new FinishMessage());
                }
            }

            public class FinishHandler: IHandleMessages<FinishMessage>
            {
                public Context Context { get; set; }

                public void Handle(FinishMessage message)
                {
                    Context.Done = true;
                }
            }

            public class Saga1 : Saga<Saga1.Saga1Data>, IAmStartedByMessages<StartSaga>, IHandleMessages<MessageToSaga>
            {

                public void Handle(StartSaga message)
                {
                }

                public void Handle(MessageToSaga message)
                {
                }

                public class Saga1Data : ContainSagaData
                {
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<Saga1Data> mapper)
                {
                }
            }

            public class Saga2 : Saga<Saga2.Saga2Data>, IAmStartedByMessages<StartSaga>, IHandleMessages<MessageToSaga>
            {

                public void Handle(StartSaga message)
                {
                }

                public void Handle(MessageToSaga message)
                {
                }

                public class Saga2Data : ContainSagaData
                {
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<Saga2Data> mapper)
                {
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
                EndpointSetup<DefaultServer>();
            }

            class EnsureOrdering : ISpecifyMessageHandlerOrdering
            {
                public void SpecifyOrder(Order order)
                {
                    order.Specify(First<Saga1>.Then<Saga2>());
                }
            }

            public class MessageToSagaHandler : IHandleMessages<MessageToSaga>
            {
                public IBus Bus { get; set; }

                public void Handle(MessageToSaga message)
                {
                    Bus.Defer(TimeSpan.FromSeconds(10), new FinishMessage());
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

            public class Saga1 : Saga<Saga1.Saga1Data>, IAmStartedByMessages<StartSaga>, IHandleMessages<MessageToSaga>
            {

                public void Handle(StartSaga message)
                {
                }

                public void Handle(MessageToSaga message)
                {
                }

                public class Saga1Data : ContainSagaData
                {
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<Saga1Data> mapper)
                {
                }
            }

            public class Saga2 : Saga<Saga2.Saga2Data>, IHandleMessages<StartSaga>, IAmStartedByMessages<MessageToSaga>
            {

                public void Handle(StartSaga message)
                {
                }

                public void Handle(MessageToSaga message)
                {
                }

                public class Saga2Data : ContainSagaData
                {
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<Saga2Data> mapper)
                {
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
        }

        [Serializable]
        public class FinishMessage : ICommand
        {
        }

        [Serializable]
        public class MessageToSaga : ICommand
        {
        }
    }
}