namespace NServiceBus.Saga
{
    using System;

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
        void Save(IContainSagaData saga);

        /// <summary>
        /// Updates an existing saga entity in the persistence store.
        /// </summary>
        /// <param name="saga">The saga entity to updated.</param>
        void Update(IContainSagaData saga);

		/// <summary>
		/// Gets a saga entity from the persistence store by its Id.
		/// </summary>
		/// <param name="sagaId">The Id of the saga entity to get.</param>
        T Get<T>(Guid sagaId) where T : IContainSagaData;

        /// <summary>
        /// Looks up a saga entity by a given property.
        /// </summary>
        T Get<T>(string property, object value) where T : IContainSagaData;

		/// <summary>
        /// Sets a saga as completed and removes it from the active saga list
		/// in the persistence store.
		/// </summary>
		/// <param name="saga">The saga to complete.</param>
        void Complete(IContainSagaData saga);
    }

    /// <summary>
    /// Interface responsible for persisting sagas.
    /// </summary>
    public interface IPersistSagas : ISagaPersister {}
}
