namespace NServiceBus.AcceptanceTests.Core.Pipeline;

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

        Assert.That(context.InterceptingHandlerCalledFirst, Is.True, "The intercepting message handler should be called first");
    }

    public class SagaEndpointContext : ScenarioContext
    {
        public bool InterceptingHandlerCalled { get; set; }
        public bool InterceptingHandlerCalledFirst { get; set; }
        public bool SagaStarted { get; set; }
    }

    public class SagaEndpoint : EndpointConfigurationBuilder
    {
        public SagaEndpoint() => EndpointSetup<DefaultServer>(b => b.ExecuteTheseHandlersFirst(typeof(InterceptingHandler)));

        public class TestSaga13(SagaEndpointContext testContext) : Saga<TestSaga13.TestSagaData13>, IAmStartedByMessages<StartSagaMessage>
        {
            public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
            {
                if (testContext.InterceptingHandlerCalled)
                {
                    testContext.InterceptingHandlerCalledFirst = true;
                }
                testContext.SagaStarted = true;
                return Task.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData13> mapper) =>
                mapper.ConfigureMapping<StartSagaMessage>(m => m.SomeId)
                    .ToSaga(s => s.SomeId);

            public class TestSagaData13 : ContainSagaData
            {
                public virtual string SomeId { get; set; }
            }
        }

        public class InterceptingHandler(SagaEndpointContext testContext) : IHandleMessages<StartSagaMessage>
        {
            public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
            {
                testContext.InterceptingHandlerCalled = true;

                return Task.CompletedTask;
            }
        }
    }

    public class StartSagaMessage : ICommand
    {
        public string SomeId { get; set; }
    }
}