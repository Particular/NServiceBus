namespace NServiceBus.AcceptanceTests.Core.Sagas;

using System;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

[TestFixture]
public class When_mapping_saga_messages_using_base_classes : NServiceBusAcceptanceTest
{
    [Test, CancelAfter(20_000)]
    public async Task Should_apply_base_class_mapping_to_sub_classes(CancellationToken cancellationToken = default)
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
            .Run(cancellationToken);

        Assert.That(context.SecondMessageFoundExistingSaga, Is.True);
    }

    public class Context : ScenarioContext
    {
        public bool SecondMessageFoundExistingSaga { get; set; }
    }

    public class SagaEndpoint : EndpointConfigurationBuilder
    {
        public SagaEndpoint() => EndpointSetup<DefaultServer>();

        public class BaseClassIsMappedSaga(Context testContext) : Saga<BaseClassIsMappedSaga.BaseClassIsMappedSagaData>,
            IAmStartedByMessages<StartSagaMessage>,
            IAmStartedByMessages<SecondSagaMessage>
        {
            public Task Handle(SecondSagaMessage message, IMessageHandlerContext context)
            {
                testContext.SecondMessageFoundExistingSaga = true;
                return Task.CompletedTask;
            }

            public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
            {
                var sagaMessage = new SecondSagaMessage
                {
                    SomeId = message.SomeId
                };
                return context.SendLocal(sagaMessage);
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<BaseClassIsMappedSagaData> mapper) =>
                mapper.MapSaga(s => s.SomeId)
                    .ToMessage<SagaMessageBase>(m => m.SomeId);

            public class BaseClassIsMappedSagaData : ContainSagaData
            {
                public virtual Guid SomeId { get; set; }
            }
        }
    }

    public class StartSagaMessage : SagaMessageBase;

    public class SecondSagaMessage : SagaMessageBase;

    public class SagaMessageBase : IMessage
    {
        public Guid SomeId { get; set; }
    }
}