namespace NServiceBus.Transports.ActiveMQ.Receivers
{
    using System.Collections.Generic;
    using Apache.NMS;

    public class ActiveMqPurger : IActiveMqPurger
    {
        private readonly HashSet<IDestination> destinations;

        public ActiveMqPurger()
        {
            destinations = new HashSet<IDestination>();
        }

        public void Purge(ISession session, IDestination destination)
        {
            lock (destinations)
            {
                if (!destinations.Contains(destination))
                {
                    session.DeleteDestination(destination);

                    destinations.Add(destination);
                }
            }
        }
    }
}