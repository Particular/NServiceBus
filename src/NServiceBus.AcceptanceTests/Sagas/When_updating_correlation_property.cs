namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_updating_correlation_property : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_blow_up()
        {
            await Scenario.Define<Context>()
                .WithEndpoint<ChangePropertyEndpoint>(b => b.When(bus => bus.SendLocalAsync(new StartSagaMessage
                {
                    SomeId = Guid.NewGuid()
                })))
                .AllowExceptions(ex => ex.Message.Contains("Changing the value of correlated properties at runtime is currently not supported"))
                .Done(c => c.Exceptions.Any())
                .Run();
        }

        public class Context : ScenarioContext
        {
            public bool SecondMessageReceived { get; set; }

            public Guid FirstSagaId { get; set; }
            public Guid SecondSagaId { get; set; }
        }

        public class ChangePropertyEndpoint : EndpointConfigurationBuilder
        {
            public ChangePropertyEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class ChangeCorrPropertySaga : Saga<ChangeCorrPropertySagaData>, IAmStartedByMessages<StartSagaMessage>
            {
                public Context Context { get; set; }

                public Task Handle(StartSagaMessage message)
                {
                    if (message.SecondMessage)
                    {
                        Data.SomeId = Guid.NewGuid(); //this is not allowed
                    }
                    else
                    {
                        Data.SomeId = message.SomeId;
                    }


                    return Bus.SendLocalAsync(new StartSagaMessage
                    {
                        SomeId = message.SomeId,
                        SecondMessage = true
                    });
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<ChangeCorrPropertySagaData> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                }
            }

            public class ChangeCorrPropertySagaData : IContainSagaData
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

            public bool SecondMessage { get; set; }
        }
    }
}