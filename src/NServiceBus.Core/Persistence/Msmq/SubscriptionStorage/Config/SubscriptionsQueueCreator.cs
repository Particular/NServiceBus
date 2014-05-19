namespace NServiceBus.Persistence.Msmq.SubscriptionStorage.Config
{
    using Unicast.Queuing;

    class SubscriptionsQueueCreator : IWantQueueCreated
    {
        public Address Address
        {
            get { return ConfigureMsmqSubscriptionStorage.Queue; }
        }

        public bool ShouldCreateQueue(Configure config)
        {
            return ConfigureMsmqSubscriptionStorage.Queue != null;
        }
    }
}
