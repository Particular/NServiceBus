namespace NServiceBus.AcceptanceTests.Sagas;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_saga_exists_for_start_message : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_hydrate_and_invoke_the_existing_instance()
    {
        var someId = Guid.NewGuid();
        var context = await Scenario.Define<Context>()
            .WithEndpoint<ExistingSagaInstanceEndpoint>(b => b
                .When(async session =>
                {
                    await session.SendLocal(
                        new StartSagaMessage
                        {
                            SomeId = someId
                        });
                    await session.SendLocal(
                        new StartSagaMessage
                        {
                            SomeId = someId
                        });
                }))
            .Run();

        Assert.That(context.SagaIds, Has.Count.EqualTo(2));
        Assert.That(context.SagaIds[1], Is.EqualTo(context.SagaIds[0]));
    }

    public class Context : ScenarioContext
    {
        public IList<Guid> SagaIds { get; } = [];
    }

    public class ExistingSagaInstanceEndpoint : EndpointConfigurationBuilder
    {
        public ExistingSagaInstanceEndpoint() => EndpointSetup<DefaultServer>(c => c.LimitMessageProcessingConcurrencyTo(1));

        [Saga]
        public class TestSaga05(Context testContext) : Saga<TestSagaData05>, IAmStartedByMessages<StartSagaMessage>
        {
            public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
            {
                testContext.SagaIds.Add(Data.Id);
                testContext.MarkAsCompleted(testContext.SagaIds.Count >= 2);
                return Task.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData05> mapper) =>
                mapper.MapSaga(s => s.SomeId)
                    .ToMessage<StartSagaMessage>(m => m.SomeId);
        }

        public class TestSagaData05 : ContainSagaData
        {
            public virtual Guid SomeId { get; set; }
        }
    }

    public class StartSagaMessage : ICommand
    {
        public Guid SomeId { get; set; }
    }
}