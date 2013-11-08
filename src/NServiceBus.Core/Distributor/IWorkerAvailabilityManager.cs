namespace NServiceBus.Distributor
{
    /// <summary>
    ///     Defines a manager class that determines the availability
    ///     of a worker for the <see cref="DistributorSatellite" />.
    /// </summary>
    public interface IWorkerAvailabilityManager
    {
        /// <summary>
        /// Signal that a worker is available to receive a dispatched message.
        /// </summary>
        /// <param name="worker">The worker details.</param>
        void WorkerAvailable(Worker worker);

        /// <summary>
        /// Registers a new worker with <see cref="IWorkerAvailabilityManager"/>.
        /// </summary>
        /// <param name="worker">The worker details.</param>
        /// <param name="capacity">The number of messages that this worker is ready to process.</param>
        void RegisterNewWorker(Worker worker, int capacity);

        /// <summary>
        /// Unregisters the worker from the <see cref="IWorkerAvailabilityManager"/>.
        /// </summary>
        /// <param name="address"><see cref="Address"/> of worker to unregister.</param>
        void UnregisterWorker(Address address);

        /// <summary>
        ///     Retrieves the next available worker from the available worker list.
        /// </summary>
        /// <returns>The <see cref="Worker"/> details, or <value>null</value> if no worker is available.</returns>
        Worker NextAvailableWorker();
    }
}