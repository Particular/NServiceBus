namespace NServiceBus.AcceptanceTests.Sagas;

using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NServiceBus;
using NUnit.Framework;

class When_correlation_property_is_int : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Show_allow_default()
    {
        var context = await Scenario.Define<DefaultIntCorrelationIdContext>()
            .WithEndpoint<EndpointWithIntSaga>(e => e
                .When(s => s
                    .SendLocal(new MessageWithIntCorrelationProperty
                    {
                        CorrelationProperty = default
                    })))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.MessageCorrelated, Is.True);
            Assert.That(context.CorrelatedId, Is.Zero);
        }
    }

    public class MessageWithIntCorrelationProperty : IMessage
    {
        public int CorrelationProperty { get; set; }
    }

    public class RequestWithIntCorrelationProperty : IMessage
    {
        public int RequestedId { get; set; }
    }

    public class EndpointWithIntSaga : EndpointConfigurationBuilder
    {
        public EndpointWithIntSaga() => EndpointSetup<DefaultServer>();

        public class SagaDataWithIntCorrelatedProperty : ContainSagaData
        {
            public virtual int CorrelatedProperty { get; set; }
        }

        [Saga]
        public class SagaWithIntCorrelatedProperty(DefaultIntCorrelationIdContext scenarioContext)
            : Saga<SagaDataWithIntCorrelatedProperty>,
                IAmStartedByMessages<MessageWithIntCorrelationProperty>,
                IHandleMessages<RequestWithIntCorrelationProperty>
        {
            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaDataWithIntCorrelatedProperty> mapper) =>
                mapper.MapSaga(s => s.CorrelatedProperty)
                    .ToMessage<MessageWithIntCorrelationProperty>(msg => msg.CorrelationProperty)
                    .ToMessage<RequestWithIntCorrelationProperty>(msg => msg.RequestedId);

            public Task Handle(MessageWithIntCorrelationProperty message, IMessageHandlerContext context) =>
                context.SendLocal(new RequestWithIntCorrelationProperty
                {
                    RequestedId = Data.CorrelatedProperty
                });

            public Task Handle(RequestWithIntCorrelationProperty message, IMessageHandlerContext context)
            {
                scenarioContext.MessageCorrelated = true;
                scenarioContext.CorrelatedId = Data.CorrelatedProperty;
                scenarioContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class DefaultIntCorrelationIdContext : ScenarioContext
    {
        public bool MessageCorrelated { get; set; }
        public int CorrelatedId { get; set; }
    }
}