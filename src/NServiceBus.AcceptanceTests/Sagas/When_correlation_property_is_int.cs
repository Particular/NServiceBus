namespace NServiceBus.AcceptanceTests.Sagas
{
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
                .Done(c => c.MessageCorrelated)
                .Run();

            Assert.That(context.MessageCorrelated, Is.True);
            Assert.That(context.CorrelatedId, Is.EqualTo(default(int)));
        }

        public class MessageWithIntCorrelationProperty : IMessage
        {
            public int CorrelationProperty { get; set; }
        }

        public class RequestWithIntCorrelationProperty : IMessage
        {
            public int RequestedId { get; set; }
        }

        class EndpointWithIntSaga : EndpointConfigurationBuilder
        {
            public EndpointWithIntSaga()
            {
                EndpointSetup<DefaultServer>();
            }

            public class SagaDataWithIntCorrelatedProperty : ContainSagaData
            {
                public virtual int CorrelatedProperty { get; set; }
            }

            class SagaWithIntCorrelatedProperty : Saga<SagaDataWithIntCorrelatedProperty>, 
                IAmStartedByMessages<MessageWithIntCorrelationProperty>, 
                IHandleMessages<RequestWithIntCorrelationProperty>
            {
                DefaultIntCorrelationIdContext scenarioContext;

                public SagaWithIntCorrelatedProperty(DefaultIntCorrelationIdContext scenarioContext)
                {
                    this.scenarioContext = scenarioContext;
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaDataWithIntCorrelatedProperty> mapper)
                {
                    mapper.ConfigureMapping<MessageWithIntCorrelationProperty>(msg => msg.CorrelationProperty)
                        .ToSaga(saga => saga.CorrelatedProperty);

                    mapper.ConfigureMapping<RequestWithIntCorrelationProperty>(msg => msg.RequestedId)
                        .ToSaga(saga => saga.CorrelatedProperty);
                }

                public Task Handle(MessageWithIntCorrelationProperty message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    return context.SendLocal(new RequestWithIntCorrelationProperty
                    {
                        RequestedId = Data.CorrelatedProperty
                    });
                }

                public Task Handle(RequestWithIntCorrelationProperty message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    scenarioContext.MessageCorrelated = true;
                    scenarioContext.CorrelatedId = Data.CorrelatedProperty;
                    return Task.FromResult(0);
                }
            }
        }

        class DefaultIntCorrelationIdContext : ScenarioContext
        {
            public bool MessageCorrelated { get; set; }
            public int CorrelatedId { get; set; }
        }
    }
}