namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Features;
    using NUnit.Framework;

    [TestFixture]
    public class When_a_base_class_message_hits_a_saga
    {
        [Test]
        public async Task Should_find_existing_instance()
        {
            var correlationId = Guid.NewGuid();
            var context = await Scenario.Define<Context>()
                   .WithEndpoint<SagaEndpoint>(b => b.When(bus => bus.SendLocal(new StartSagaMessage
                   {
                       SomeId = correlationId
                   })))
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
                EndpointSetup<DefaultServer>(c => c.EnableFeature<TimeoutManager>());
            }

            public class TestSaga04 : Saga<TestSaga04.SagaData04>, IAmStartedByMessages<StartSagaMessageBase>
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
                        return context.SendLocal(new StartSagaMessage
                        {
                            SomeId = message.SomeId
                        });
                    }

                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData04> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessageBase>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                }

                public class SagaData04 : ContainSagaData
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