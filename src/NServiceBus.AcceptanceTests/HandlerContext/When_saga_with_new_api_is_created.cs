namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Saga;
    using NUnit.Framework;

    public class When_saga_with_new_api_is_created : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_process_the_saga_successfully()
        {
            var dataId = Guid.NewGuid();
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<SagaEndpoint>(b =>
                    b.Given(bus =>
                    {
                        bus.SendLocal(new StartSaga
                                       {
                                           DataId = dataId
                                       });
                    })
                )
                .Done(c => c.SagaCompleted)
                .Run();

            Assert.IsTrue(context.SagaCompleted);
            Assert.IsTrue(context.SagaStarted);
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

            public class Saga1 : Saga<Saga1.Saga1Data>,
                IAmStartedByMessage<StartSaga>,
                IHandleTimeout<Saga1.Timeout1>
            {
                public Context Context { get; set; }

                public void Handle(StartSaga message, IHandleContext context)
                {
                    Data.DataId = message.DataId;

                    Context.SagaStarted = true;

                    //Request a timeout
                    RequestTimeout<Timeout1>(TimeSpan.FromSeconds(5));
                }

                public void Timeout(Timeout1 state, ITimeoutContext context)
                {
                    MarkAsComplete();
                    Context.SagaCompleted = true;
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
                }
            }
        }

        [Serializable]
        public class StartSaga : ICommand
        {
            public Guid DataId { get; set; }
        }
    }
}
