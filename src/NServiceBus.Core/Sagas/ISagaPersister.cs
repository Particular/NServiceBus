namespace NServiceBus.Sagas
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;

    /// <summary>
    /// Defines the basic functionality of a persister for storing 
	/// and retrieving a saga.
    /// </summary>
    public interface ISagaPersister
    {
        /// <summary>
        /// Saves the saga entity to the persistence store.
        /// </summary>
        /// <param name="saga">The saga entity to save.</param>
        /// <param name="metadata">Metadata about this saga type.</param>
        /// <param name="context">The current pipeline context.</param>
        Task Save(IContainSagaData saga,SagaMetadata metadata, ContextBag context);

        /// <summary>
        /// Updates an existing saga entity in the persistence store.
        /// </summary>
        /// <param name="saga">The saga entity to updated.</param>
        /// <param name="context">The current pipeline context.</param>
        Task Update(IContainSagaData saga, ContextBag context);

        /// <summary>
        /// Gets a saga entity from the persistence store by its Id.
        /// </summary>
        /// <param name="sagaId">The Id of the saga entity to get.</param>
        /// <param name="context">The current pipeline context.</param>
        Task<TSagaData> Get<TSagaData>(Guid sagaId, ContextBag context) where TSagaData : IContainSagaData;

        /// <summary>
        /// Looks up a saga entity by a given property.
        /// </summary>
        /// <param name="propertyName">From the data store, analyze this property.</param>
        /// <param name="propertyValue">From the data store, look for this value in the identified property.</param>
        /// <param name="context">The current pipeline context.</param>
        Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, ContextBag context) where TSagaData : IContainSagaData;

        /// <summary>
        /// Sets a saga as completed and removes it from the active saga list
        /// in the persistence store.
        /// </summary>
        /// <param name="saga">The saga to complete.</param>
        /// <param name="context">The current pipeline context.</param>
        Task Complete(IContainSagaData saga, ContextBag context);
    }
}
