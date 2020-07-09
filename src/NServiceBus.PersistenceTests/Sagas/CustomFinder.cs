namespace NServiceBus.PersistenceTests.Sagas
{
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Sagas;
    using Persistence;

    public class CustomFinder : IFindSagas<SagaWithoutCorrelationPropertyData>.Using<SagaWithoutCorrelationPropertyStartingMessage>
    {
        public Task<SagaWithoutCorrelationPropertyData> FindBy(SagaWithoutCorrelationPropertyStartingMessage message, SynchronizedStorageSession storageSession, ReadOnlyContextBag context)
        {
            return Task.FromResult(default(SagaWithoutCorrelationPropertyData));
        }
    }
}