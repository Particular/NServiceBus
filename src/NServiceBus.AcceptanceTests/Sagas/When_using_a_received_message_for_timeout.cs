namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using Saga;

    public class When_using_a_received_message_for_timeout : NServiceBusAcceptanceTest
    {
        [Test]
        public void Timeout_should_be_received_after_expiration()
        {
            Scenario.Define(() => new Context {Id = Guid.NewGuid()})
                    .WithEndpoint<SagaEndpoint>(g => g.Given(bus=>bus.SendLocal(new StartSagaMessage())))
                    .Done(c => c.TimeoutReceived)
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public Guid Id { get; set; }

            public bool StartSagaMessageReceived { get; set; }

            public bool TimeoutReceived { get; set; }
        }

        public class SagaEndpoint : EndpointConfigurationBuilder
        {
            public SagaEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class TestSaga : Saga<TestSagaData>, IAmStartedByMessages<StartSagaMessage>, IHandleTimeouts<StartSagaMessage>
            {
                public Context Context { get; set; }

                public void Handle(StartSagaMessage message)
                {
                    Data.SomeId = message.SomeId;
                    RequestTimeout(TimeSpan.FromMilliseconds(100), message);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                }

                public void Timeout(StartSagaMessage message)
                {
                    Context.TimeoutReceived = true;
                    MarkAsComplete();
                }
            }

            public class TestSagaData : IContainSagaData
            {
                public virtual Guid Id { get; set; }
                public virtual string Originator { get; set; }
                public virtual string OriginalMessageId { get; set; }

                [Unique]
                public virtual Guid SomeId { get; set; }
            }
        }

        public class StartSagaMessage : ICommand
        {
            public Guid SomeId { get; set; }
        }
    }
}