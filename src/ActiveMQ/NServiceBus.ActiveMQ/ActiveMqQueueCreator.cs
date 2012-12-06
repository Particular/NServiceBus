namespace NServiceBus.ActiveMQ
{
    using NServiceBus.Unicast.Queuing;

    public class ActiveMqQueueCreator : ICreateQueues
    {
        public void CreateQueueIfNecessary(Address address, string account)
        {
            // no op
        }
    }
}