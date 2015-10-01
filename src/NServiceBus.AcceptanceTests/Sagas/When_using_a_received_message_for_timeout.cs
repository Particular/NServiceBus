namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Features;
    using NUnit.Framework;

    public class When_using_a_received_message_for_timeout : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Timeout_should_be_received_after_expiration()
        {
            await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                    .WithEndpoint<RecvMsgForTimeoutEndpt>(g => g.When(bus => bus.SendLocalAsync(new StartSagaMessage())))
                    .Done(c => c.TimeoutReceived)
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public Guid Id { get; set; }

            public bool StartSagaMessageReceived { get; set; }

            public bool TimeoutReceived { get; set; }
        }

        public class RecvMsgForTimeoutEndpt : EndpointConfigurationBuilder
        {
            public RecvMsgForTimeoutEndpt()
            {
                EndpointSetup<DefaultServer>(config => config.EnableFeature<TimeoutManager>());
            }

            public class TestSaga01 : Saga<TestSagaData01>, IAmStartedByMessages<StartSagaMessage>, IHandleTimeouts<StartSagaMessage>
            {
                public Context Context { get; set; }

                public Task Handle(StartSagaMessage message)
                {
                    Data.SomeId = message.SomeId;
                    return RequestTimeoutAsync(TimeSpan.FromMilliseconds(100), message);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData01> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                }

                public Task Timeout(StartSagaMessage message)
                {
                    MarkAsComplete();
                    Context.TimeoutReceived = true;
                    return Task.FromResult(0);
                }
            }

            public class TestSagaData01 : IContainSagaData
            {
                public virtual Guid Id { get; set; }
                public virtual string Originator { get; set; }
                public virtual string OriginalMessageId { get; set; }
                public virtual Guid SomeId { get; set; }
            }
        }

        public class StartSagaMessage : ICommand
        {
            public Guid SomeId { get; set; }
        }
    }
}