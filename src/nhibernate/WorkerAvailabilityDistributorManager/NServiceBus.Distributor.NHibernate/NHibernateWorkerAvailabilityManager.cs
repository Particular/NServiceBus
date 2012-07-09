namespace NServiceBus.Distributor.NHibernate
{
    using Config;
    using Unicast.Distributor;
    using global::NHibernate;
    using IsolationLevel = System.Data.IsolationLevel;

    /// <summary>
    /// NHibernate implementation of <see cref="IWorkerAvailabilityManager"/>.
    /// </summary>
    public class NHibernateWorkerAvailabilityManager : IWorkerAvailabilityManager
    {
        private string currentEndpointName;

        /// <summary>
        /// Creates <c>ISession</c>s.
        /// </summary>
        public ISessionFactory SessionFactory { get; set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public NHibernateWorkerAvailabilityManager()
        {
            currentEndpointName = Configure.EndpointName;
        }

        /// <summary>
        /// Start the worker availability manager
        /// </summary>
        public void Start()
        {
            
        }

        /// <summary>
        /// Signal that a worker is available to receive a dispatched message.
        /// </summary>
        /// <param name="address">The address of the worker that will accept the dispatched message.</param>
        /// <param name="capacity">The number of messages that this worker is ready to process</param>
        public void WorkerAvailable(Address address, int capacity)
        {
            using (var session = SessionFactory.OpenStatelessSession())
            using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                for (var i = 0; i < capacity; i++)
                {
                    session.Insert(new DistributorMessage
                                       {
                                           Destination = address,
                                           Endpoint = currentEndpointName,
                                       });
                }

                tx.Commit();
            }
        }

        /// <summary>
        /// Pops the next available worker from the available worker list and returns its address.
        /// </summary>
        /// <returns>
        /// The address of the next available worker.
        /// </returns>
        public Address PopAvailableWorker()
        {
            using (var session = SessionFactory.OpenSession())
            using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                var message = session.QueryOver<DistributorMessage>()
                    .Where(x => x.Endpoint == currentEndpointName)
                    .Take(1)
                    .SingleOrDefault();

                if (message != null)
                {
                    session.Delete(message);
                }

                tx.Commit();

                return message != null ? message.Destination : null;
            }
        }

        /// <summary>
        /// Removes all entries from the worker availability list with the specified address.
        /// </summary>
        /// <param name="address">The address of the worker to remove from the availability list.</param>
        public void ClearAvailabilityForWorker(Address address)
        {
            using (var session = SessionFactory.OpenStatelessSession())
            using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                var queryString = string.Format("delete {0} where Destination = :address and Endpoint = :endpoint",
                                        typeof(DistributorMessage));
                session.CreateQuery(queryString)
                       .SetParameter("address", address)
                       .SetParameter("endpoint", currentEndpointName)
                       .ExecuteUpdate();

                tx.Commit();
            }
        }
    }
}
