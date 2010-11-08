using System;

namespace NServiceBus.Saga
{
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
        void Save(ISagaEntity saga);

        /// <summary>
        /// Updates an existing saga entity in the persistence store.
        /// </summary>
        /// <param name="saga">The saga entity to updated.</param>
        void Update(ISagaEntity saga);

		/// <summary>
		/// Gets a saga entity from the persistence store by its Id.
		/// </summary>
		/// <param name="sagaId">The Id of the saga entity to get.</param>
		/// <returns></returns>
        T Get<T>(Guid sagaId) where T : ISagaEntity;

        /// <summary>
        /// Looks up a saga entity by a given property.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="property"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        T Get<T>(string property, object value) where T : ISagaEntity;

		/// <summary>
        /// Sets a saga as completed and removes it from the active saga list
		/// in the persistence store.
		/// </summary>
		/// <param name="saga">The saga to complete.</param>
        void Complete(ISagaEntity saga);
    }

    /// <summary>
    /// Interface responsible for persisting sagas.
    /// </summary>
    public interface IPersistSagas : ISagaPersister {}
}
