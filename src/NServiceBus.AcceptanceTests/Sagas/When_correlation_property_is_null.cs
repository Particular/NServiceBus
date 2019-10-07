namespace NServiceBus.AcceptanceTests.Sagas
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Support;
    using EndpointTemplates;
    using NUnit.Framework;

    class When_correlation_property_is_null : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_throw_an_exception_with_details()
        {
            var exception = Assert.ThrowsAsync<MessageFailedException>(async () => await Scenario.Define<ScenarioContext>()
                .WithEndpoint<SagaWithCorrelationPropertyEndpoint>(e => e
                    .When(s => s
                        .SendLocal(new MessageWithNullCorrelationProperty
                        {
                            CorrelationProperty = null
                        })))
                .Done(c => c.FailedMessages.Count > 0)
                .Run());

            var errorMessage = $"Message {typeof(MessageWithNullCorrelationProperty).FullName} mapped to saga {typeof(SagaWithCorrelationPropertyEndpoint.SagaWithCorrelatedProperty).FullName} has attempted to assign null to the correlation property {nameof(SagaWithCorrelationPropertyEndpoint.SagaDataWithCorrelatedProperty.CorrelatedProperty)}. Correlation properties cannot be assigned null.";

            StringAssert.Contains(errorMessage, exception.FailedMessage.Exception.Message);
        }

        public class SagaWithCorrelationPropertyEndpoint : EndpointConfigurationBuilder
        {
            public SagaWithCorrelationPropertyEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class SagaDataWithCorrelatedProperty : ContainSagaData
            {
                public virtual string CorrelatedProperty { get; set; }
            }

            public class SagaWithCorrelatedProperty : Saga<SagaDataWithCorrelatedProperty>, IAmStartedByMessages<MessageWithNullCorrelationProperty>
            {
                public Task Handle(MessageWithNullCorrelationProperty message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaDataWithCorrelatedProperty> mapper)
                {
                    mapper.ConfigureMapping<MessageWithNullCorrelationProperty>(m => m.CorrelationProperty).ToSaga(s => s.CorrelatedProperty);
                }
            }
        }

        public class MessageWithNullCorrelationProperty : ICommand
        {
            public string CorrelationProperty { get; set; }
        }
    }
}