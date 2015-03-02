namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Saga;
    using NUnit.Framework;

    public class When_saga_with_old_and_new_api_is_created : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_process_the_saga_successfully()
        {
            var dataId = Guid.NewGuid();

            Scenario.Define<Context>()
                .WithEndpoint<SagaEndpoint>(b =>
                    b.Given(bus =>
                    {
                        bus.SendLocal(new StartSaga
                                       {
                                           DataId = dataId
                                       });
                    })
                    .When(c => c.SagaStarted, (bus, c) => { bus.Publish<SomethingHappenedEvent>(m => m.DataId = dataId); })
                )
                .Done(c => c.SagaCompleted)
                .Run(TimeSpan.FromSeconds(10));
        }

        public class Context : ScenarioContext
        {
            public bool SagaCompleted { get; set; }
            public bool SagaStarted { get; set; }
        }

        public class SagaEndpoint : EndpointConfigurationBuilder
        {
            public SagaEndpoint()
            {
                EndpointSetup<DefaultPublisher>();
            }

            public class Saga1 : Saga<Saga1.Saga1Data>, // Daniel: I think we need a new base class which doesn't have the IBus already predefined or should we use the same base and advice users to use the context information?
                IAmStartedByMessages<StartSaga>, // Daniel: I think we shouldn't allow this. But technically it is possible
                IAmStartedByMessage<StartSaga>, // Daniel: see above you need to pick and choose
                IAmStartedByEvent<SomethingHappenedEvent>, // Daniel: I'm started by event?
                IHandleTimeouts<Saga1.Timeout1>, // Daniel: I think we shouldn't allow this. But technically it is possible
                IHandleTimeout<Saga1.Timeout1> // Daniel: see above you need to pick and choose
            {
                public Context Context { get; set; }

                public void Handle(StartSaga message, HandleContext context)
                {
                }

                public void Handle(StartSaga message)
                {
                    Data.DataId = message.DataId;

                    Context.SagaStarted = true;

                    //Request a timeout
                    RequestTimeout<Timeout1>(TimeSpan.FromSeconds(5));
                }

                public void Timeout(Timeout1 state)
                {
                    MarkAsComplete();
                    Context.SagaCompleted = true;
                }

                public void Timeout(Timeout1 state, TimeoutContext context)
                {
                    MarkAsComplete();
                    Context.SagaCompleted = true;
                }

                public void Handle(SomethingHappenedEvent message, SubscribeContext context)
                {
                }

                public class Saga1Data : ContainSagaData
                {
                    [Unique]
                    public virtual Guid DataId { get; set; }
                }

                public class Timeout1
                {
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<Saga1Data> mapper)
                {
                    mapper.ConfigureMapping<SomethingHappenedEvent>(m => m.DataId).ToSaga(d => d.DataId);
                }
            }
        }

        [Serializable]
        public class StartSaga : ICommand
        {
            public Guid DataId { get; set; }
        }

        public interface SomethingHappenedEvent : BaseEvent
        {
        }

        public interface BaseEvent : IEvent
        {
            Guid DataId { get; set; }
        }
    }
}
