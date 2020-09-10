namespace NServiceBus.AcceptanceTests.Core.Sagas
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    [TestFixture]
    public class When_mapping_saga_messages_using_base_classes : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_apply_base_class_mapping_to_sub_classes()
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

            public class BaseClassIsMappedSaga : Saga<BaseClassIsMappedSaga.BaseClassIsMappedSagaData>,
                IAmStartedByMessages<StartSagaMessage>,
                IAmStartedByMessages<SecondSagaMessage>
            {
                public BaseClassIsMappedSaga(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(SecondSagaMessage message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    testContext.SecondMessageFoundExistingSaga = true;
                    return Task.FromResult(0);
                }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    var sagaMessage = new SecondSagaMessage
                    {
                        SomeId = message.SomeId
                    };
                    return context.SendLocal(sagaMessage);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<BaseClassIsMappedSagaData> mapper)
                {
                    mapper.ConfigureMapping<SagaMessageBase>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                }

                public class BaseClassIsMappedSagaData : ContainSagaData
                {
                    public virtual Guid SomeId { get; set; }
                }

                Context testContext;
            }
        }

        public class StartSagaMessage : SagaMessageBase
        {
        }

        public class SecondSagaMessage : SagaMessageBase
        {
        }

        public class SagaMessageBase : IMessage
        {
            public Guid SomeId { get; set; }
        }
    }
}