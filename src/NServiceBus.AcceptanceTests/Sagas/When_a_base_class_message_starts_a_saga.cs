namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    [TestFixture]
    public class When_a_base_class_message_starts_a_saga : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_find_existing_instance()
        {
            var correlationId = Guid.NewGuid();
            var context = await Scenario.Define<Context>()
                .WithEndpoint<SagaEndpoint>(b => b.When(session =>
                {
                    var startSagaMessage = new StartSagaMessage
                    {
                        SomeId = correlationId
                    };
                    return session.SendLocal(startSagaMessage);
                }))
                .Done(c => c.SecondMessageFoundExistingSaga)
                .Run(TimeSpan.FromSeconds(20));

            Assert.True(context.SecondMessageFoundExistingSaga);
        }

        public class Context : ScenarioContext
        {
            public bool SecondMessageFoundExistingSaga { get; set; }
        }

        public class SagaEndpoint : EndpointConfigurationBuilder
        {
            public SagaEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class BaseClassStartsSaga : Saga<BaseClassStartsSaga.BaseClassStartsSagaData>,
                IAmStartedByMessages<StartSagaMessageBase>
            {
                public Context TestContext { get; set; }

                public Task Handle(StartSagaMessageBase message, IMessageHandlerContext context)
                {
                    if (Data.SomeId != Guid.Empty)
                    {
                        TestContext.SecondMessageFoundExistingSaga = true;
                    }
                    else
                    {
                        var startSagaMessage = new StartSagaMessage
                        {
                            SomeId = message.SomeId
                        };
                        return context.SendLocal(startSagaMessage);
                    }

                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<BaseClassStartsSagaData> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessageBase>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                }

                public class BaseClassStartsSagaData : ContainSagaData
                {
                    public virtual Guid SomeId { get; set; }
                }
            }
        }

        public class StartSagaMessage : StartSagaMessageBase
        {
        }

        public class StartSagaMessageBase : IMessage
        {
            public Guid SomeId { get; set; }
        }
    }
}