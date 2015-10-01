namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;

    public class When_a_existing_saga_instance_exists : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_hydrate_and_invoke_the_existing_instance()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<ExistingSagaInstanceEndpt>(b => b.When(bus => bus.SendLocalAsync(new StartSagaMessage { SomeId = Guid.NewGuid() })))
                .Done(c => c.SecondMessageReceived)
                .Run();

            Assert.AreEqual(context.FirstSagaId, context.SecondSagaId, "The same saga instance should be invoked invoked for both messages");
        }

        public class Context : ScenarioContext
        {
            public bool SecondMessageReceived { get; set; }

            public Guid FirstSagaId { get; set; }
            public Guid SecondSagaId { get; set; }
        }

        public class ExistingSagaInstanceEndpt : EndpointConfigurationBuilder
        {
            public ExistingSagaInstanceEndpt()
            {
                EndpointSetup<DefaultServer>();
            }

            public class TestSaga05 : Saga<TestSagaData05>, IAmStartedByMessages<StartSagaMessage>
            {
                public Context Context { get; set; }
                public Task Handle(StartSagaMessage message)
                {
                    Data.SomeId = message.SomeId;

                    if (message.SecondMessage)
                    {
                        Context.SecondSagaId = Data.Id;
                        Context.SecondMessageReceived = true;
                    }
                    else
                    {
                        Context.FirstSagaId = Data.Id;
                        return Bus.SendLocalAsync(new StartSagaMessage { SomeId = message.SomeId, SecondMessage = true });
                    }

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
                public virtual Guid Id { get; set; }
                public virtual string Originator { get; set; }
                public virtual string OriginalMessageId { get; set; }
                public virtual Guid SomeId { get; set; }
            }
        }

        [Serializable]
        public class StartSagaMessage : ICommand
        {
            public Guid SomeId { get; set; }

            public bool SecondMessage { get; set; }
        }
    }
}