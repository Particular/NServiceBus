namespace NServiceBus.Transports.ActiveMQ.Receivers
{
    using System.Collections.Generic;
    using Apache.NMS;

    public class ActiveMqPurger : IActiveMqPurger
    {
        private readonly HashSet<IDestination> destinations;

        public ActiveMqPurger()
        {
            this.destinations = new HashSet<IDestination>();
        }

        public void Purge(ISession session, IDestination destination)
        {
            lock (this.destinations)
            {
                if (!this.destinations.Contains(destination))
                {
                    session.DeleteDestination(destination);

                    this.destinations.Add(destination);
                }
            }
        }
    }
}