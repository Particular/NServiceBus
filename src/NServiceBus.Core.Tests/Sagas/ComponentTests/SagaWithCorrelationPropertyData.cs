namespace NServiceBus.Persistence.ComponentTests
{
    using System;
    using System.Threading.Tasks;

    public class SagaWithCorrelationProperty : Saga<SagaWithCorrelationPropertyData>, IAmStartedByMessages<SagaCorrelationPropertyStartingMessage>
    {
        public Task Handle(SagaCorrelationPropertyStartingMessage message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaWithCorrelationPropertyData> mapper)
        {
            mapper.ConfigureMapping<SagaCorrelationPropertyStartingMessage>(m => m.CorrelatedProperty).ToSaga(s => s.CorrelatedProperty);
        }
    }
    public class SagaWithCorrelationPropertyData : ContainSagaData
    {
        public string CorrelatedProperty { get; set; }

        public DateTime DateTimeProperty { get; set; }
    }

    public class SagaCorrelationPropertyStartingMessage
    {
        public string CorrelatedProperty { get; set; }
    }
}