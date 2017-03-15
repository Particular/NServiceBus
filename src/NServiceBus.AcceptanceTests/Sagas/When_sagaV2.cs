namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_sagaV2 : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_execute_saga()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<SagaEndpoint>(b => b
                    .When(session => session.SendLocal(new StartSagaMessage
                    {
                        Key = "Part1_Part2"
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
                EndpointSetup<DefaultServer>();
            }

            public class ASagaV2 : SagaV2<ASagaV2.SagaV2Data>,
                IAmStartedByMessages<StartSagaMessage>,
                IAmStartedByMessages<OtherMessage>
            {

                Context context;

                public ASagaV2(Context context)
                {
                    this.context = context;
                }

                protected override string CorrelationPropertyName => nameof(SagaV2Data.CorrelationId);

                protected override void ConfigureMapping(IMessagePropertyMapper mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.Key);
                    mapper.ConfigureMapping<OtherMessage>(m => $"{m.Part1}_{m.Part2}");
                }

                public Task Handle(OtherMessage message, IMessageHandlerContext handlerContext)
                {
                    Assert.AreEqual(context.SagaId, Data.Id, "Existing instance should be found");
                    context.SecondMessageReceived = true;
                    return Task.FromResult(0);
                }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext handlerContext)
                {
                    context.FirstMessageReceived = true;
                    context.SagaId = Data.Id;

                    var otherMessage = new OtherMessage
                    {
                        Part1 = "Part1",
                        Part2 = "Part2"
                    };
                    return handlerContext.SendLocal(otherMessage);
                }

                public class SagaV2Data : IContainSagaData
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