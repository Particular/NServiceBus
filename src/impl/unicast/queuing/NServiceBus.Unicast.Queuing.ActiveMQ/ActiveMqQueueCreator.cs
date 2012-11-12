namespace NServiceBus.Unicast.Queuing.ActiveMQ
{
    public class NullQueueCreator : ICreateQueues
    {
         public void CreateQueueIfNecessary(Address address, string account)
         {
         }
    }
}