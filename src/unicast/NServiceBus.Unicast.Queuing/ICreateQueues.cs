namespace NServiceBus.Unicast.Queuing
{
    public interface ICreateQueues
    {
        void CreateQueueIfNecessary(Address address, string account);
    }
}