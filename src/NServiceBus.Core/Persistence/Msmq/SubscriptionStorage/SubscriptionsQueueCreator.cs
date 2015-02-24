namespace NServiceBus.Persistence.SubscriptionStorage
{
    using Unicast.Queuing;

    class SubscriptionsQueueCreator : IWantQueueCreated
    {
        public string StorageQueue { get; set; }

        public string Address
        {
            get { return StorageQueue; }
        }

        public bool ShouldCreateQueue()
        {
            return StorageQueue != null;
        }
    }
}
