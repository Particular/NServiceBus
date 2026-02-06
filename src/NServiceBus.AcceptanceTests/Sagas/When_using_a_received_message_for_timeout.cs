namespace NServiceBus.AcceptanceTests.Sagas;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_using_a_received_message_for_timeout : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Timeout_should_be_received_after_expiration()
    {
        Requires.DelayedDelivery();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<ReceiveMessageForTimeoutEndpoint>(g => g.When(session => session.SendLocal(new StartSagaMessage
            {
                SomeId = Guid.NewGuid()
            })))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.TimeoutReceived, Is.True);
            Assert.That(context.HandlerCalled, Is.EqualTo(1));
        }
    }

    public class Context : ScenarioContext
    {
        public bool TimeoutReceived { get; set; }
        public int HandlerCalled { get; set; }
    }

    public class ReceiveMessageForTimeoutEndpoint : EndpointConfigurationBuilder
    {
        public ReceiveMessageForTimeoutEndpoint() => EndpointSetup<DefaultServer>();

        [Saga]
        public class TestSaga01(Context testContext) : Saga<TestSagaData01>, IAmStartedByMessages<StartSagaMessage>,
            IHandleTimeouts<StartSagaMessage>
        {
            public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
            {
                testContext.HandlerCalled++;
                return RequestTimeout(context, TimeSpan.FromMilliseconds(100), message);
            }

            public Task Timeout(StartSagaMessage message, IMessageHandlerContext context)
            {
                MarkAsComplete();
                testContext.TimeoutReceived = true;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData01> mapper) =>
                mapper.MapSaga(s => s.SomeId)
                    .ToMessage<StartSagaMessage>(m => m.SomeId);
        }

        public class TestSagaData01 : ContainSagaData
        {
            public virtual Guid SomeId { get; set; }
        }
    }

    public class StartSagaMessage : ICommand
    {
        public Guid SomeId { get; set; }
    }
}