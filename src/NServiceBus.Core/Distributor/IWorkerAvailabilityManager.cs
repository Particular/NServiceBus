namespace NServiceBus.Distributor
{
    /// <summary>
    ///     Defines a manager class that determines the availability
    ///     of a worker for the <see cref="DistributorSatellite" />.
    /// </summary>
    public interface IWorkerAvailabilityManager
    {
        void WorkerAvailable(Worker worker);

        void RegisterNewWorker(Worker worker, int capacity);

        void DisconnectWorker(Address address);

        /// <summary>
        ///     Pops the next available worker from the available worker list
        ///     and returns its address.
        /// </summary>
        /// <returns>The address of the next available worker.</returns>
        Worker NextAvailableWorker();
    }

    public class Worker
    {
        public Worker(Address address, string sessionId)
        {
            Address = address;
            SessionId = sessionId;
        }

        public Address Address { get; set; }
        public string SessionId { get; set; }
    }
}