namespace NServiceBus.AcceptanceTests.Sagas;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NServiceBus;
using NUnit.Framework;

class When_correlation_property_is_guid : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Show_allow_default()
    {
        var context = await Scenario.Define<GuidCorrelationIdContext>()
            .WithEndpoint<EndpointWithGuidSaga>(e => e
                .When(s => s
                    .SendLocal(new MessageWithGuidCorrelationProperty
                    {
                        CorrelationProperty = default
                    })))
            .Done(c => c.MessageCorrelated)
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.MessageCorrelated, Is.True);
            Assert.That(context.CorrelatedId, Is.Default);
        }
    }

    public class MessageWithGuidCorrelationProperty : IMessage
    {
        public Guid CorrelationProperty { get; set; }
    }

    public class RequestWithGuidCorrelationProperty : IMessage
    {
        public Guid RequestedId { get; set; }
    }

    class EndpointWithGuidSaga : EndpointConfigurationBuilder
    {
        public EndpointWithGuidSaga() => EndpointSetup<DefaultServer>();

        public class SagaDataWithGuidCorrelatedProperty : ContainSagaData
        {
            public virtual Guid CorrelatedProperty { get; set; }
        }

        class SagaWithGuidCorrelatedProperty(GuidCorrelationIdContext scenarioContext)
            : Saga<SagaDataWithGuidCorrelatedProperty>,
                IAmStartedByMessages<MessageWithGuidCorrelationProperty>,
                IHandleMessages<RequestWithGuidCorrelationProperty>
        {
            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaDataWithGuidCorrelatedProperty> mapper) =>
                mapper.MapSaga(s => s.CorrelatedProperty)
                    .ToMessage<MessageWithGuidCorrelationProperty>(msg => msg.CorrelationProperty)
                    .ToMessage<RequestWithGuidCorrelationProperty>(msg => msg.RequestedId);

            public Task Handle(MessageWithGuidCorrelationProperty message, IMessageHandlerContext context) =>
                context.SendLocal(new RequestWithGuidCorrelationProperty
                {
                    RequestedId = Data.CorrelatedProperty
                });

            public Task Handle(RequestWithGuidCorrelationProperty message, IMessageHandlerContext context)
            {
                scenarioContext.MessageCorrelated = true;
                scenarioContext.CorrelatedId = Data.CorrelatedProperty;
                return Task.CompletedTask;
            }
        }
    }

    class GuidCorrelationIdContext : ScenarioContext
    {
        public bool MessageCorrelated { get; set; }
        public Guid CorrelatedId { get; set; }
    }
}