namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using Saga;

    public class When_receiving_that_should_start_a_saga : NServiceBusAcceptanceTest
    {
        public class SagaEndpointContext : ScenarioContext
        {
            public bool InterceptingHandlerCalled { get; set; }

            public bool SagaStarted { get; set; }

            public bool InterceptSaga { get; set; }
        }

        public class SagaEndpoint : EndpointConfigurationBuilder
        {
            public SagaEndpoint()
            {
                EndpointSetup<DefaultServer>(b => b.LoadMessageHandlers<First<InterceptingHandler>>());
            }

            public class TestSaga : Saga<TestSaga.TestSagaData>, IAmStartedByMessages<StartSagaMessage>
            {
                public SagaEndpointContext Context { get; set; }
                public void Handle(StartSagaMessage message)
                {
                    Context.SagaStarted = true;
                    Data.SomeId = message.SomeId;
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m=>m.SomeId)
                        .ToSaga(s=>s.SomeId);
                }

                public class TestSagaData : ContainSagaData
                {
                    public virtual string SomeId { get; set; }
                }
            }

            
            public class InterceptingHandler : IHandleMessages<StartSagaMessage>
            {
                public SagaEndpointContext Context { get; set; }

                public IBus Bus { get; set; }

                public void Handle(StartSagaMessage message)
                {
                    Context.InterceptingHandlerCalled = true;

                    if (Context.InterceptSaga)
                        Bus.DoNotContinueDispatchingCurrentMessageToHandlers();
                }
            }
        }

        [Serializable]
        public class StartSagaMessage : ICommand
        {
            public string SomeId { get; set; }
        }
    }
}
