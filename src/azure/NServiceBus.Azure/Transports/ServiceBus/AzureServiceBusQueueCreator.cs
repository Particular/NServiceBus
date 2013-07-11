namespace NServiceBus.Unicast.Queuing.Azure.ServiceBus
{
    using Transports;

    /// <summary>
    /// Creates the queues. Note that this class will only be invoked when running the windows host and not when running in the fabric
    /// </summary>
    public class AzureServiceBusQueueCreator:ICreateQueues
    {
        public ICreateQueueClients CreateQueueClients { get; set; }

        public void CreateQueueIfNecessary(Address address, string account)
        {
            CreateQueueClients.Create(address);
        }
    }
}