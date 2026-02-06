namespace NServiceBus.AcceptanceTests.Sagas;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_saga_has_a_non_empty_constructor : NServiceBusAcceptanceTest
{
    [Test]
    public Task Should_hydrate_and_invoke_the_existing_instance() =>
        Scenario.Define<Context>()
            .WithEndpoint<NonEmptySagaCtorEndpt>(b => b.When(session => session.SendLocal(new StartSagaMessage
            {
                SomeId = IdThatSagaIsCorrelatedOn
            })))
            .Run();

    static Guid IdThatSagaIsCorrelatedOn = Guid.NewGuid();

    public class Context : ScenarioContext
    {
        public bool SecondMessageReceived { get; set; }
    }

    public class NonEmptySagaCtorEndpt : EndpointConfigurationBuilder
    {
        public NonEmptySagaCtorEndpt() => EndpointSetup<DefaultServer>();

        [Saga]
        public class TestSaga11(Context testContext) : Saga<TestSagaData11>,
            IAmStartedByMessages<StartSagaMessage>,
            IHandleMessages<OtherMessage>
        {
            public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
            {
                Data.SomeId = message.SomeId;
                return context.SendLocal(new OtherMessage
                {
                    SomeId = message.SomeId
                });
            }

            public Task Handle(OtherMessage message, IMessageHandlerContext context)
            {
                testContext.SecondMessageReceived = true;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData11> mapper) =>
                mapper.MapSaga(s => s.SomeId)
                    .ToMessage<StartSagaMessage>(m => m.SomeId)
                    .ToMessage<OtherMessage>(m => m.SomeId);
        }

        public class TestSagaData11 : ContainSagaData
        {
            public virtual Guid SomeId { get; set; }
        }
    }


    public class StartSagaMessage : ICommand
    {
        public Guid SomeId { get; set; }
    }


    public class OtherMessage : ICommand
    {
        public Guid SomeId { get; set; }
    }
}