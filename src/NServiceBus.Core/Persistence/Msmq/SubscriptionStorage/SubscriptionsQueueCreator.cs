namespace NServiceBus.Persistence.SubscriptionStorage
{
    using Unicast.Queuing;

    class SubscriptionsQueueCreator : IWantQueueCreated
    {
        public Address StorageQueue { get; set; }

        public Address Address
        {
            get { return StorageQueue; }
        }

        public bool ShouldCreateQueue()
        {
            return StorageQueue != null;
        }
    }
}
