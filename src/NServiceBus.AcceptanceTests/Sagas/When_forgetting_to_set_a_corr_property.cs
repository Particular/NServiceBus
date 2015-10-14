namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    //note: this test will be obsolete if/when we implement https://github.com/Particular/NServiceBus/issues/313
    public class When_forgetting_to_set_a_corr_property : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_blow_up()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<NullPropertyEndpoint>(b => b.When(bus => bus.SendLocalAsync(new StartSagaMessage
                {
                    SomeId = Guid.NewGuid()
                })))
                .AllowExceptions()
                .Done(c => c.Exceptions.Any())
                .Run();

            Assert.True(context.Exceptions.Any(ex => ex.Message.Contains("All correlated properties must have a non null or empty value assigned to them when a new saga instance is created")));
        }

        public class Context : ScenarioContext
        {
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
                    return Task.FromResult(0);
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

        [Serializable]
        public class StartSagaMessage : ICommand
        {
            public Guid SomeId { get; set; }
        }
    }
}