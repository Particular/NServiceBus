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
                        .When(bus => bus.SendLocalAsync(new StartSagaMessage { Key = "Part1_Part2"}))
                        .When(c => c.FirstMessageReceived, bus =>  bus.SendLocalAsync(new OtherMessage { Part1 = "Part1", Part2 = "Part2" })))
                    .AllowExceptions()
                    .Done(c => c.SecondMessageReceived)
                    .Run();

            Assert.IsTrue(context.SecondMessageReceived);
        }

        public class Context : ScenarioContext
        {
            public bool FirstMessageReceived { get; set; }
            public bool SecondMessageReceived { get; set; }
        }

        public class SagaEndpoint : EndpointConfigurationBuilder
        {
            public SagaEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class TestSaga02 : Saga<TestSagaData02>,
                IAmStartedByMessages<StartSagaMessage>, IAmStartedByMessages<OtherMessage>
            {
                public Context Context { get; set; }
                public Task Handle(StartSagaMessage message)
                {
                    Data.KeyValue = message.Key;
                    Context.FirstMessageReceived = true;
                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData02> mapper)
                {
                    mapper.ConfigureMapping<OtherMessage>(m => m.Part1 + "_" + m.Part2)
                        .ToSaga(s => s.KeyValue);
                }

                public Task Handle(OtherMessage message)
                {
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

        [Serializable]
        public class StartSagaMessage : ICommand
        {
            public string Key { get; set; }
        }
        [Serializable]
        public class OtherMessage : ICommand
        {
            public string Part2 { get; set; }
            public string Part1 { get; set; }
        }
    }
}