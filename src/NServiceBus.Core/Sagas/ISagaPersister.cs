namespace NServiceBus.Sagas
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using Persistence;

    /// <summary>
    /// Defines the basic functionality of a persister for storing
    /// and retrieving a sagaData.
    /// </summary>
    public interface ISagaPersister
    {
        /// <summary>
        /// Saves the sagaData entity to the persistence store.
        /// </summary>
        /// <param name="sagaData">The sagaData data to save.</param>
        /// <param name="correlationProperty">The property to correlate. Can be null.</param>
        /// <param name="session">Storage session.</param>
        /// <param name="context">The current pipeline context.</param>
        Task Save(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, SynchronizedStorageSession session, ContextBag context);

        /// <summary>
        /// Updates an existing sagaData entity in the persistence store.
        /// </summary>
        /// <param name="sagaData">The sagaData data to updated.</param>
        /// <param name="session">The session.</param>
        /// <param name="context">The current pipeline context.</param>
        Task Update(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context);

        /// <summary>
        /// Gets a sagaData entity from the persistence store by its Id.
        /// </summary>
        /// <param name="sagaId">The Id of the sagaData data to get.</param>
        /// <param name="session">The session.</param>
        /// <param name="context">The current pipeline context.</param>
        Task<TSagaData> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session, ContextBag context) where TSagaData : IContainSagaData;

        /// <summary>
        /// Looks up a sagaData entity by a given property.
        /// </summary>
        /// <param name="propertyName">From the data store, analyze this property.</param>
        /// <param name="propertyValue">From the data store, look for this value in the identified property.</param>
        /// <param name="session">The session.</param>
        /// <param name="context">The current pipeline context.</param>
        Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, ContextBag context) where TSagaData : IContainSagaData;

        /// <summary>
        /// Sets a sagaData as completed and removes it from the active sagaData list
        /// in the persistence store.
        /// </summary>
        /// <param name="sagaData">The sagaData to complete.</param>
        /// <param name="session">The session.</param>
        /// <param name="context">The current pipeline context.</param>
        Task Complete(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context);
    }
}