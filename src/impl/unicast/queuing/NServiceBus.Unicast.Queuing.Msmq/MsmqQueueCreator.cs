using NServiceBus.Utils;

namespace NServiceBus.Unicast.Queuing.Msmq
{
    public class MsmqQueueCreator : ICreateQueues
    {
        public void CreateQueueIfNecessary(Address address, string account)
        {
            MsmqUtilities.CreateQueueIfNecessary(address, account);
        }
    }
}