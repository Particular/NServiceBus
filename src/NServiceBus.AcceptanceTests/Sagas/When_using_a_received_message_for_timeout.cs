namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    public class When_using_a_received_message_for_timeout : NServiceBusAcceptanceTest
    {
        [Test]
        public Task Timeout_should_be_received_after_expiration()
        {
            return Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                .WithEndpoint<ReceiveMessageForTimeoutEndpoint>(g => g.When(session => session.SendLocal(new StartSagaMessage
                {
                    SomeId = Guid.NewGuid()
                })))
                .Done(c => c.TimeoutReceived)
                .Run();
        }

        public class Context : ScenarioContext
        {
            public Guid Id { get; set; }

            public bool StartSagaMessageReceived { get; set; }

            public bool TimeoutReceived { get; set; }
        }

        public class ReceiveMessageForTimeoutEndpoint : EndpointConfigurationBuilder
        {
            public ReceiveMessageForTimeoutEndpoint()
            {
                EndpointSetup<DefaultServer>(config => config.EnableFeature<TimeoutManager>());
            }

            public class TestSaga01 : Saga<TestSagaData01>, IAmStartedByMessages<StartSagaMessage>, IHandleTimeouts<StartSagaMessage>
            {
                public Context TestContext { get; set; }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    Data.SomeId = message.SomeId;
                    return RequestTimeout(context, TimeSpan.FromMilliseconds(100), message);
                }

                public Task Timeout(StartSagaMessage message, IMessageHandlerContext context)
                {
                    MarkAsComplete();
                    TestContext.TimeoutReceived = true;
                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData01> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                }
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