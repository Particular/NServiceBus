namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using EndpointTemplates;
    using AcceptanceTesting;

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
                EndpointSetup<DefaultServer>(b => b.ExecuteTheseHandlersFirst(typeof(InterceptingHandler)));
            }

            public class TestSaga03 : Saga<TestSaga03.TestSagaData03>, IAmStartedByMessages<StartSagaMessage>
            {
                public SagaEndpointContext Context { get; set; }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    Context.SagaStarted = true;
                    Data.SomeId = message.SomeId;
                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData03> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                }

                public class TestSagaData03 : ContainSagaData
                {
                    public virtual string SomeId { get; set; }
                }
            }


            public class InterceptingHandler : IHandleMessages<StartSagaMessage>
            {
                public SagaEndpointContext TestContext { get; set; }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    TestContext.InterceptingHandlerCalled = true;

                    if (TestContext.InterceptSaga)
                        context.DoNotContinueDispatchingCurrentMessageToHandlers();

                    return Task.FromResult(0);
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
