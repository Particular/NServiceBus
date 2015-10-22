namespace NServiceBus.Sagas
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Persistence;

    /// <summary>
    /// Defines the basic functionality of a persister for storing 
	/// and retrieving a saga.
    /// </summary>
    public interface ISagaPersister
    {
        /// <summary>
        /// Saves the saga entity to the persistence store.
        /// </summary>
        /// <param name="sagaInstance">The saga instance to save.</param>
        /// <param name="correlationProperty">The property to correlate. Can be null.</param>
        /// <param name="session">Storage session.</param>
        /// <param name="context">The current pipeline context.</param>
        Task Save(IContainSagaData sagaInstance, SagaCorrelationProperty correlationProperty, SynchronizedStorageSession session, ContextBag context);

        /// <summary>
        /// Updates an existing saga entity in the persistence store.
        /// </summary>
        /// <param name="saga">The saga entity to updated.</param>
        /// <param name="session">The session.</param>
        /// <param name="context">The current pipeline context.</param>
        Task Update(IContainSagaData saga, SynchronizedStorageSession session, ContextBag context);

        /// <summary>
        /// Gets a saga entity from the persistence store by its Id.
        /// </summary>
        /// <param name="sagaId">The Id of the saga entity to get.</param>
        /// <param name="session">The session.</param>
        /// <param name="context">The current pipeline context.</param>
        Task<TSagaData> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session, ContextBag context) where TSagaData : IContainSagaData;

        /// <summary>
        /// Looks up a saga entity by a given property.
        /// </summary>
        /// <param name="propertyName">From the data store, analyze this property.</param>
        /// <param name="propertyValue">From the data store, look for this value in the identified property.</param>
        /// <param name="session">The session.</param>
        /// <param name="context">The current pipeline context.</param>
        Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, ContextBag context) where TSagaData : IContainSagaData;

        /// <summary>
        /// Sets a saga as completed and removes it from the active saga list
        /// in the persistence store.
        /// </summary>
        /// <param name="saga">The saga to complete.</param>
        /// <param name="session">The session.</param>
        /// <param name="context">The current pipeline context.</param>
        Task Complete(IContainSagaData saga, SynchronizedStorageSession session, ContextBag context);
    }
}
