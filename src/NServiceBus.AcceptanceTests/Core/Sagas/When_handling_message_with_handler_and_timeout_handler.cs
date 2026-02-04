namespace NServiceBus.AcceptanceTests.Core.Sagas;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_handling_message_with_handler_and_timeout_handler : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_not_invoke_timeout_handler()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<TimeoutSagaEndpoint>(g => g.When(session => session.SendLocal(new StartSagaMessage
            {
                SomeId = Guid.NewGuid()
            })))
            .Run();

        Assert.That(context.HandlerInvoked, Is.True, "Regular handler should be invoked");
    }

    public class Context : ScenarioContext
    {
        public bool HandlerInvoked { get; set; }
    }

    public class TimeoutSagaEndpoint : EndpointConfigurationBuilder
    {
        public TimeoutSagaEndpoint() => EndpointSetup<DefaultServer>();

        [Saga]
        public class HandlerAndTimeoutSaga(Context testContext) : Saga<HandlerAndTimeoutSagaData>,
            IAmStartedByMessages<StartSagaMessage>,
            IHandleTimeouts<StartSagaMessage>
        {
            public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
            {
                testContext.HandlerInvoked = true;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }

            public Task Timeout(StartSagaMessage message, IMessageHandlerContext context)
            {
                testContext.MarkAsFailed(new InvalidOperationException("Timeout handler invoked"));
                return Task.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<HandlerAndTimeoutSagaData> mapper) =>
                mapper.MapSaga(s => s.SomeId)
                    .ToMessage<StartSagaMessage>(m => m.SomeId);
        }

        public class HandlerAndTimeoutSagaData : ContainSagaData
        {
            public virtual Guid SomeId { get; set; }
        }
    }

    public class StartSagaMessage : IMessage
    {
        public Guid SomeId { get; set; }
    }
}