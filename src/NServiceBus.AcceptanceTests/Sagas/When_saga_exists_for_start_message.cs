namespace NServiceBus.AcceptanceTests.Sagas
{
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
                .Done(c => c.SagaIds.Count >= 2)
                .Run();

            Assert.AreEqual(2, context.SagaIds.Count);
            Assert.AreEqual(context.SagaIds[0], context.SagaIds[1]);
        }

        public class Context : ScenarioContext
        {
            public IList<Guid> SagaIds { get; } = new List<Guid>();
        }

        public class ExistingSagaInstanceEndpoint : EndpointConfigurationBuilder
        {
            public ExistingSagaInstanceEndpoint()
            {
                EndpointSetup<DefaultServer>(c => c.LimitMessageProcessingConcurrencyTo(1));
            }

            public class TestSaga05 : Saga<TestSagaData05>, IAmStartedByMessages<StartSagaMessage>
            {
                public Context TestContext { get; set; }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    TestContext.SagaIds.Add(Data.Id);

                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData05> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                }
            }

            public class TestSagaData05 : IContainSagaData
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

            public bool SecondMessage { get; set; }
        }
    }
}