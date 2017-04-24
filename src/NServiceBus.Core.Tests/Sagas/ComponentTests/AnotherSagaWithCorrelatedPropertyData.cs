namespace NServiceBus.Persistence.ComponentTests
{
    using System;
    using System.Threading.Tasks;

    class AnotherSagaWithCorrelatedProperty : Saga<AnotherSagaWithCorrelatedPropertyData>, IAmStartedByMessages<TwoUniquePropertyStartingMessage>
    {
        public Task Handle(TwoUniquePropertyStartingMessage message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<AnotherSagaWithCorrelatedPropertyData> mapper)
        {
            mapper.ConfigureMapping<TwoUniquePropertyStartingMessage>(m => m.CorrelatedProperty).ToSaga(s => s.CorrelatedProperty);
        }
    }

    class TwoUniquePropertyStartingMessage
    {
        public string CorrelatedProperty { get; set; }
    }

    public class AnotherSagaWithCorrelatedPropertyData : ContainSagaData
    {
        public string CorrelatedProperty { get; set; }
    }
}