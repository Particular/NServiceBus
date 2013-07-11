namespace NServiceBus.Transports.ActiveMQ
{
    public class ActiveMqQueueCreator : ICreateQueues
    {
        public void CreateQueueIfNecessary(Address address, string account)
        {
            // no op
        }
    }
}