namespace NServiceBus.AutomaticSubscriptions.Config
{
    using NServiceBus.Routing.AutomaticSubscriptions;

    /// <summary>
    /// Provides fine grained control over auto subscribe
    /// </summary>
    public class AutoSubscribeSettings
    {
        BusConfiguration config;

        internal AutoSubscribeSettings(BusConfiguration config)
        {
            this.config = config;
        }

        /// <summary>
        /// Turns off auto subscriptions for sagas. Sagas where not auto subscribed by default before v4
        /// </summary>
        public void DoNotAutoSubscribeSagas()
        {
            config.Settings.SetProperty<AutoSubscriptionStrategy>(c => c.DoNotAutoSubscribeSagas, true);
        }

        /// <summary>
        /// Allows to endpoint to subscribe to messages owned by the local endpoint
        /// </summary>
        public void DoNotRequireExplicitRouting()
        {
            config.Settings.SetProperty<AutoSubscriptionStrategy>(c => c.DoNotRequireExplicitRouting, true); 
        }

        /// <summary>
        /// Turns on auto-subscriptions for messages not marked as commands. This was the default before v4
        /// </summary>
        public void AutoSubscribePlainMessages()
        {
            config.Settings.SetProperty<AutoSubscriptionStrategy>(c => c.SubscribePlainMessages, true);
        }
    }
}