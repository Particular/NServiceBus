namespace NServiceBus.Persistence.Msmq.SubscriptionStorage.Config
{
    using NServiceBus.Unicast.Queuing;

    /// <summary>
    /// Signals to create MSMQ subscription queue
    /// </summary>
    public class SubscriptionsQueueCreator : IWantQueueCreated
    {
        /// <summary>
        /// MSMQ Subscription storage
        /// </summary>
        public Address Address
        {
            get { return ConfigureMsmqSubscriptionStorage.Queue; }
        }

        /// <summary>
        /// Disabling the creation of the MSMQ Subscription queue
        /// </summary>
        public bool IsDisabled
        {
            get { return ConfigureMsmqSubscriptionStorage.Queue == null; }
        }
    }
}
