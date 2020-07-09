namespace NServiceBus.PersistenceTests.Sagas
{
    using System;
    using System.Threading.Tasks;

    public class SagaWithoutCorrelationProperty : Saga<SagaWithoutCorrelationPropertyData>,
        IAmStartedByMessages<SagaWithoutCorrelationPropertyStartingMessage>
    {
        public Task Handle(SagaWithoutCorrelationPropertyStartingMessage message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaWithoutCorrelationPropertyData> mapper)
        {
            // no mapping needed
        }
    }
}