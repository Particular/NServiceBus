namespace NServiceBus.Persistence.ComponentTests
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using Sagas;

    class SagaWithoutCorrelationProperty : Saga<SagaWithoutCorrelationPropertyData>, 
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

    public class CustomFinder : IFindSagas<SagaWithoutCorrelationPropertyData>.Using<SagaWithoutCorrelationPropertyStartingMessage>
    {
        public Task<SagaWithoutCorrelationPropertyData> FindBy(SagaWithoutCorrelationPropertyStartingMessage message, SynchronizedStorageSession storageSession, ReadOnlyContextBag context)
        {
            return Task.FromResult(default(SagaWithoutCorrelationPropertyData));
        }
    }

    public class SagaWithoutCorrelationPropertyData : ContainSagaData
    {
        public string FoundByFinderProperty { get; set; }
    }

    public class SagaWithoutCorrelationPropertyStartingMessage : IMessage
    {
        public string FoundByFinderProperty { get; set; }
    }
}