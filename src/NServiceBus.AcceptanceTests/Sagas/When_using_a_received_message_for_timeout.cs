namespace NServiceBus.AcceptanceTests.Sagas
{
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
                .Done(c => c.TimeoutReceived)
                .Run();

            Assert.True(context.TimeoutReceived);
            Assert.AreEqual(1, context.HandlerCalled);
        }

        public class Context : ScenarioContext
        {
            public bool TimeoutReceived { get; set; }
            public int HandlerCalled { get; set; }
        }

        public class ReceiveMessageForTimeoutEndpoint : EndpointConfigurationBuilder
        {
            public ReceiveMessageForTimeoutEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class TestSaga01 : Saga<TestSagaData01>, IAmStartedByMessages<StartSagaMessage>, IHandleTimeouts<StartSagaMessage>
            {
                public TestSaga01(Context context)
                {
                    testContext = context;
                }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    testContext.HandlerCalled++;
                    return RequestTimeout(context, TimeSpan.FromMilliseconds(100), message);
                }

                public Task Timeout(StartSagaMessage message, IMessageHandlerContext context)
                {
                    MarkAsComplete();
                    testContext.TimeoutReceived = true;
                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData01> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                }

                Context testContext;
            }

            public class TestSagaData01 : IContainSagaData
            {
                public virtual Guid SomeId { get; set; }
                public virtual Guid Id { get; set; }
                public virtual string Originator { get; set; }
                public virtual string OriginalMessageId { get; set; }
            }
        }

        public class StartSagaMessage : ICommand
        {
            public Guid SomeId { get; set; }
        }
    }
}