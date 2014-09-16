namespace NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions
{
    internal class StorageInitializer : IWantToRunWhenBusStartsAndStops
    {
        public ISubscriptionStorage SubscriptionStorage { get; set; }

        public void Start()
        {
            if (SubscriptionStorage != null)
            {
                SubscriptionStorage.Init();
            }
        }

        public void Stop()
        {
        }
    }
}