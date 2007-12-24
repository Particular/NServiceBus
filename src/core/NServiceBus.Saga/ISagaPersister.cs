using System;

namespace NServiceBus.Saga
{
    /// <summary>
    /// Defines the basic functionality of a persister for storing 
	/// and retrieving a saga.
    /// </summary>
	/// <remarks>
	/// Use per-instance (single-call) semantics for instantiation rather than singleton.
	/// </remarks>
    public interface ISagaPersister : IDisposable
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
        ISagaEntity Get(Guid sagaId);

		/// <summary>
        /// Sets a saga as completed and removes it from the active saga list
		/// in the persistence store.
		/// </summary>
		/// <param name="saga">The saga to complete.</param>
        void Complete(ISagaEntity saga);
    }
}
