namespace NServiceBus.Unicast.Queuing.Msmq
{
    using Utils;
    using Config;

    public class MsmqQueueCreator : ICreateQueues
    {
        public void CreateQueueIfNecessary(Address address, string account)
        {
            MsmqUtilities.CreateQueueIfNecessary(address, account, Endpoint.IsVolatile);
        }
    }
}