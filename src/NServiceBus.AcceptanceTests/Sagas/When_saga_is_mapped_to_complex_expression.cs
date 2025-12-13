namespace NServiceBus.AcceptanceTests.Sagas;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_saga_is_mapped_to_complex_expression : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_hydrate_and_invoke_the_existing_instance()
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
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.SecondMessageReceived, Is.True);
            Assert.That(context.SagaIdWhenOtherMessageReceived, Is.EqualTo(context.SagaIdWhenStartSagaMessageReceived));
        }
    }

    public class Context : ScenarioContext
    {
        public bool FirstMessageReceived { get; set; }
        public bool SecondMessageReceived { get; set; }
        public Guid SagaIdWhenStartSagaMessageReceived { get; set; }
        public Guid SagaIdWhenOtherMessageReceived { get; set; }
    }

    public class SagaEndpoint : EndpointConfigurationBuilder
    {
        public SagaEndpoint() =>
            //note: the concurrency checks for the InMemory persister doesn't seem to work so limiting to 1 for now
            EndpointSetup<DefaultServer>(c => c.LimitMessageProcessingConcurrencyTo(1));

        public class TestSaga02(Context testContext) : Saga<TestSagaData02>,
            IAmStartedByMessages<StartSagaMessage>, IAmStartedByMessages<OtherMessage>
        {
            public Task Handle(OtherMessage message, IMessageHandlerContext context)
            {
                testContext.SagaIdWhenOtherMessageReceived = Data.Id;
                testContext.SecondMessageReceived = true;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }

            public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
            {
                testContext.FirstMessageReceived = true;
                testContext.SagaIdWhenStartSagaMessageReceived = Data.Id;
                return Task.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData02> mapper) =>
                mapper.MapSaga(s => s.KeyValue)
                    .ToMessage<StartSagaMessage>(m => m.Key)
                    .ToMessage<OtherMessage>(m => m.Part1 + "_" + m.Part2);
        }

        public class TestSagaData02 : ContainSagaData
        {
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