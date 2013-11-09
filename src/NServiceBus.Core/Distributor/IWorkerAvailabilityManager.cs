namespace NServiceBus.Distributor
{
	/// <summary>
	/// Defines a manager class that determines the availability
	/// of a worker for the <see cref="DistributorSatellite"/>.
	/// </summary>
    public interface IWorkerAvailabilityManager
    {
	    ///<summary>
	    /// Start the worker availability manager
	    ///</summary>
	    void Start();

        ///<summary>
        /// Stops the worker availability manager
        ///</summary>
        void Stop();

	    /// <summary>
	    /// Signal that a worker is available to receive a dispatched message.
	    /// </summary>
	    /// <param name="address">
	    /// The address of the worker that will accept the dispatched message.
	    /// </param>
	    /// <param name="capacity">The number of messages that this worker is ready to process</param>
	    void WorkerAvailable(Address address,int capacity);

		/// <summary>
		/// Pops the next available worker from the available worker list
		/// and returns its address.
		/// </summary>
		/// <returns>The address of the next available worker.</returns>
        Address PopAvailableWorker();

		/// <summary>
		/// Removes all entries from the worker availability list
		/// with the specified address.
		/// </summary>
		/// <param name="address">
		/// The address of the worker to remove from the availability list.
		/// </param>
        void ClearAvailabilityForWorker(Address address);
    }
}
