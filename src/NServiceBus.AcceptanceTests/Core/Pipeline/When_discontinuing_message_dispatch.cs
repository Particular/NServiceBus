namespace NServiceBus.AcceptanceTests.Core.Pipeline
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_discontinuing_message_dispatch : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_continue_to_dispatch_the_message()
        {
            var context = await Scenario.Define<SagaEndpointContext>()
                .WithEndpoint<SagaEndpoint>(b => b.When(session => session.SendLocal(new StartSagaMessage
                {
                    SomeId = Guid.NewGuid().ToString()
                })))
                .Done(c => c.InterceptingHandlerCalled)
                .Run();

            Assert.True(context.InterceptingHandlerCalled, "The intercepting handler should be called");
            Assert.False(context.SagaStarted, "The saga should not have been started since the intercepting handler stops the pipeline");
        }

        public class SagaEndpointContext : ScenarioContext
        {
            public bool InterceptingHandlerCalled { get; set; }

            public bool SagaStarted { get; set; }
        }

        public class SagaEndpoint : EndpointConfigurationBuilder
        {
            public SagaEndpoint()
            {
                EndpointSetup<DefaultServer>(b => b.ExecuteTheseHandlersFirst(typeof(InterceptingHandler)));
            }

            public class TestSaga03 : Saga<TestSaga03.TestSagaData03>, IAmStartedByMessages<StartSagaMessage>
            {
                public TestSaga03(SagaEndpointContext context)
                {
                    testContext = context;
                }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    testContext.SagaStarted = true;
                    Data.SomeId = message.SomeId;
                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData03> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                }

                SagaEndpointContext testContext;

                public class TestSagaData03 : ContainSagaData
                {
                    public virtual string SomeId { get; set; }
                }
            }

            public class InterceptingHandler : IHandleMessages<StartSagaMessage>
            {
                public InterceptingHandler(SagaEndpointContext context)
                {
                    testContext = context;
                }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    testContext.InterceptingHandlerCalled = true;
                    context.DoNotContinueDispatchingCurrentMessageToHandlers();

                    return Task.FromResult(0);
                }

                SagaEndpointContext testContext;
            }
        }

        public class StartSagaMessage : ICommand
        {
            public string SomeId { get; set; }
        }
    }
}