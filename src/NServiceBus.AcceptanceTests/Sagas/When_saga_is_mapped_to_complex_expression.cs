namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;

    public class When_saga_is_mapped_to_complex_expression : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_hydrate_and_invoke_the_existing_instance()
        {
            var context = await Scenario.Define<Context>()
                    .WithEndpoint<SagaEndpoint>(b => b
                        .When(bus => bus.SendLocal(new StartSagaMessage { Key = "Part1_Part2" }))
                        .When(c => c.FirstMessageReceived, bus => bus.SendLocal(new OtherMessage { Part1 = "Part1", Part2 = "Part2" })))
                    .Done(c => c.SecondMessageReceived)
                    .Run();

            Assert.IsTrue(context.SecondMessageReceived);
        }

        public class Context : ScenarioContext
        {
            public bool FirstMessageReceived { get; set; }
            public bool SecondMessageReceived { get; set; }
            public Guid SagaId { get; set; }
        }

        public class SagaEndpoint : EndpointConfigurationBuilder
        {
            public SagaEndpoint()
            {
                //note: the concurrency checks for the InMemory persister doesn't seem to work so limiting to 1 for now
                EndpointSetup<DefaultServer>(c => c.LimitMessageProcessingConcurrencyTo(1));
            }

            public class TestSaga02 : Saga<TestSagaData02>,
                IAmStartedByMessages<StartSagaMessage>, IAmStartedByMessages<OtherMessage>
            {
                public Context Context { get; set; }
                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    Context.FirstMessageReceived = true;
                    Context.SagaId = Data.Id;
                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData02> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.Key)
                        .ToSaga(s => s.KeyValue);

                    mapper.ConfigureMapping<OtherMessage>(m => m.Part1 + "_" + m.Part2)
                        .ToSaga(s => s.KeyValue);
                }

                public Task Handle(OtherMessage message, IMessageHandlerContext context)
                {
                    Assert.AreEqual(Context.SagaId, Data.Id, "Existing instance should be found");
                    Context.SecondMessageReceived = true;
                    return Task.FromResult(0);
                }
            }

            public class TestSagaData02 : IContainSagaData
            {
                public virtual Guid Id { get; set; }
                public virtual string Originator { get; set; }
                public virtual string OriginalMessageId { get; set; }
                public virtual string KeyValue { get; set; }
            }
        }

        public class StartSagaMessage : ICommand
        {
            public string Key { get; set; }
        }

        public class OtherMessage : ICommand
        {
            public string Part2 { get; set; }
            public string Part1 { get; set; }
        }
    }
}