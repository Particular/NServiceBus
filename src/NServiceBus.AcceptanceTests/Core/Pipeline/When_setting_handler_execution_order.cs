namespace NServiceBus.AcceptanceTests.Core.Pipeline
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_setting_handler_execution_order : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_call_designated_handler_first()
        {
            var context = await Scenario.Define<SagaEndpointContext>()
                .WithEndpoint<SagaEndpoint>(b => b.When(session => session.SendLocal(new StartSagaMessage
                {
                    SomeId = Guid.NewGuid().ToString()
                })))
                .Done(c => c.InterceptingHandlerCalled && c.SagaStarted)
                .Run();

            Assert.True(context.InterceptingHandlerCalledFirst, "The intercepting message handler should be called first");
        }

        public class SagaEndpointContext : ScenarioContext
        {
            public bool InterceptingHandlerCalled { get; set; }
            public bool InterceptingHandlerCalledFirst { get; set; }
            public bool SagaStarted { get; set; }
        }

        public class SagaEndpoint : EndpointConfigurationBuilder
        {
            public SagaEndpoint()
            {
                EndpointSetup<DefaultServer>(b => b.ExecuteTheseHandlersFirst(typeof(InterceptingHandler)));
            }

            public class TestSaga13 : Saga<TestSaga13.TestSagaData13>, IAmStartedByMessages<StartSagaMessage>
            {
                public SagaEndpointContext Context { get; set; }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    if (Context.InterceptingHandlerCalled)
                    {
                        Context.InterceptingHandlerCalledFirst = true;
                    }
                    Context.SagaStarted = true;
                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData13> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                }

                public class TestSagaData13 : ContainSagaData
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

                    return Task.FromResult(0);
                }
            }
        }

        public class StartSagaMessage : ICommand
        {
            public string SomeId { get; set; }
        }
    }
}