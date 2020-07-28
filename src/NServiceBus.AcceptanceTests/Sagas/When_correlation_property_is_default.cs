namespace NServiceBus.AcceptanceTests.Sagas
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NServiceBus;
    using NUnit.Framework;

    class When_correlation_property_is_default : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Show_allow_non_null_default()
        {
            var context = await Scenario.Define<DefaultCorrelationIdContext>()
                .WithEndpoint<EndpointWithSaga>(e => e
                    .When(s => s
                        .SendLocal(new MessageWithDefaultCorrelationProperty
                        {
                            CorrelationProperty = default
                        })))
                .Done(c => c.MessageCorrelated)
                .Run();

            Assert.That(context.MessageCorrelated, Is.True);
            Assert.That(context.CorrelatedId, Is.EqualTo(default(int)));
        }

        public class MessageWithDefaultCorrelationProperty : IMessage
        {
            public int CorrelationProperty { get; set; }
        }

        public class RequestWithDefaultCorrelationProperty : IMessage
        {
            public int RequestedId { get; set; }
        }

        class EndpointWithSaga : EndpointConfigurationBuilder
        {
            public EndpointWithSaga()
            {
                EndpointSetup<DefaultServer>();
            }

            public class SagaDataWithDefaultCorrelatedProperty : ContainSagaData
            {
                public virtual int CorrelatedProperty { get; set; }
            }

            class SagaWithDefaultCorrelatedProperty : Saga<SagaDataWithDefaultCorrelatedProperty>, 
                IAmStartedByMessages<MessageWithDefaultCorrelationProperty>, 
                IHandleMessages<RequestWithDefaultCorrelationProperty>
            {
                DefaultCorrelationIdContext scenarioContext;

                public SagaWithDefaultCorrelatedProperty(DefaultCorrelationIdContext scenarioContext)
                {
                    this.scenarioContext = scenarioContext;
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaDataWithDefaultCorrelatedProperty> mapper)
                {
                    mapper.ConfigureMapping<MessageWithDefaultCorrelationProperty>(msg => msg.CorrelationProperty)
                        .ToSaga(saga => saga.CorrelatedProperty);

                    mapper.ConfigureMapping<RequestWithDefaultCorrelationProperty>(msg => msg.RequestedId)
                        .ToSaga(saga => saga.CorrelatedProperty);
                }

                public Task Handle(MessageWithDefaultCorrelationProperty message, IMessageHandlerContext context)
                {
                    return context.SendLocal(new RequestWithDefaultCorrelationProperty
                    {
                        RequestedId = Data.CorrelatedProperty
                    });
                }

                public Task Handle(RequestWithDefaultCorrelationProperty message, IMessageHandlerContext context)
                {
                    scenarioContext.MessageCorrelated = true;
                    scenarioContext.CorrelatedId = Data.CorrelatedProperty;
                    return Task.FromResult(0);
                }
            }
        }

        class DefaultCorrelationIdContext : ScenarioContext
        {
            public bool MessageCorrelated { get; set; }
            public int CorrelatedId { get; set; }
        }
    }
}