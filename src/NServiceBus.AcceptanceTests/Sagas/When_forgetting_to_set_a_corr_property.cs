namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_forgetting_to_set_a_corr_property : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_matter()
        {
            var id = Guid.NewGuid();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<NullPropertyEndpoint>(b => b.When(bus => bus.SendLocal(new StartSagaMessage
                {
                    SomeId = id
                })))
                .Done(c => c.Done || c.Exceptions.Any())
                .Run();

            Assert.AreEqual(context.SomeId, id);
        }

        public class Context : ScenarioContext
        {
            public Guid SomeId { get; set; }
            public bool Done { get; set; }
        }

        public class NullPropertyEndpoint : EndpointConfigurationBuilder
        {
            public NullPropertyEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class NullCorrPropertySaga : Saga<NullCorrPropertySagaData>, IAmStartedByMessages<StartSagaMessage>
            {
                public Context Context { get; set; }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    //oops I forgot Data.SomeId = message.SomeId
                    if (message.SecondMessage)
                    {
                        Context.SomeId = Data.SomeId;
                        Context.Done = true;
                        return Task.FromResult(0);
                    }

                    return context.SendLocal(new StartSagaMessage
                    {
                        SomeId = message.SomeId,
                        SecondMessage = true
                    });
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<NullCorrPropertySagaData> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                }
            }

            public class NullCorrPropertySagaData : IContainSagaData
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