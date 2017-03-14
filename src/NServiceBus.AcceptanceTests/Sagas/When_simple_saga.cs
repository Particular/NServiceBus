namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Linq.Expressions;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_simple_saga : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_execute_saga()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<SagaEndpoint>(b => b
                    .When(session => session.SendLocal(new StartSagaMessage
                    {
                        Key = "Part1_Part2"
                    }))
                    .When(c => c.FirstMessageReceived, session => session.SendLocal(new OtherMessage
                    {
                        Part1 = "Part1",
                        Part2 = "Part2"
                    })))
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

            public class ASimpleSaga : SimpleSaga<ASimpleSaga.SagaData>,
                IAmStartedByMessages<StartSagaMessage>,
                IAmStartedByMessages<OtherMessage>
            {

                protected override Expression<Func<SagaData, object>> CorrelationProperty
                {
                    get { return data => data.CorrelationId; }
                }

                protected override void ConfigureHowToFindSaga(MessagePropertyMapper<SagaData> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.Key);
                    mapper.ConfigureMapping<OtherMessage>(m => $"{m.Part1}_{m.Part2}");
                }

                public Context Context { get; set; }

                public Task Handle(OtherMessage message, IMessageHandlerContext context)
                {
                    Assert.AreEqual(Context.SagaId, Data.Id, "Existing instance should be found");
                    Context.SecondMessageReceived = true;
                    return Task.FromResult(0);
                }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    Context.FirstMessageReceived = true;
                    Context.SagaId = Data.Id;
                    return Task.FromResult(0);
                }

                public class SagaData : IContainSagaData
                {
                    public virtual string CorrelationId { get; set; }
                    public virtual Guid Id { get; set; }
                    public virtual string Originator { get; set; }
                    public virtual string OriginalMessageId { get; set; }
                }
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