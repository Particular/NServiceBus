namespace NServiceBus.Persistence.ComponentTests
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using Sagas;

    class AnotherSagaWithoutCorrelationProperty : Saga<AnotherSagaWithoutCorrelationPropertyData>, 
        IAmStartedByMessages<AnotherSagaWithoutCorrelationPropertyStartingMessage>
    {
        public Task Handle(AnotherSagaWithoutCorrelationPropertyStartingMessage message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<AnotherSagaWithoutCorrelationPropertyData> mapper)
        {
            // no mapping needed
        }
    }

    public class AnotherCustomFinder : IFindSagas<AnotherSagaWithoutCorrelationPropertyData>.Using<AnotherSagaWithoutCorrelationPropertyStartingMessage>
    {
        public Task<AnotherSagaWithoutCorrelationPropertyData> FindBy(AnotherSagaWithoutCorrelationPropertyStartingMessage message, SynchronizedStorageSession storageSession, ReadOnlyContextBag context)
        {
            return Task.FromResult(default(AnotherSagaWithoutCorrelationPropertyData));
        }
    }

    public class AnotherSagaWithoutCorrelationPropertyData : ContainSagaData
    {
        public string FoundByFinderProperty { get; set; }
    }

    public class AnotherSagaWithoutCorrelationPropertyStartingMessage : IMessage
    {
        public string FoundByFinderProperty { get; set; }
    }
}