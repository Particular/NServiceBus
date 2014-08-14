namespace NServiceBus.AutomaticSubscriptions.Config
{
    /// <summary>
    /// Provides fine grained control over auto subscribe
    /// </summary>
    public class AutoSubscribeSettings
    {
        ConfigurationBuilder config;

        internal AutoSubscribeSettings(ConfigurationBuilder config)
        {
            this.config = config;
        }

        /// <summary>
        /// Turns off auto subscriptions for sagas. Sagas where not auto subscribed by default before v4
        /// </summary>
        public void DoNotAutoSubscribeSagas()
        {
            config.settings.SetProperty<AutoSubscriptionStrategy>(c => c.DoNotAutoSubscribeSagas, true);
        }

        /// <summary>
        /// Allows to endpoint to subscribe to messages owned by the local endpoint
        /// </summary>
        public void DoNotRequireExplicitRouting()
        {
            config.settings.SetProperty<AutoSubscriptionStrategy>(c => c.DoNotRequireExplicitRouting, true); 
        }

        /// <summary>
        /// Turns on auto-subscriptions for messages not marked as commands. This was the default before v4
        /// </summary>
        public void AutoSubscribePlainMessages()
        {
            config.settings.SetProperty<AutoSubscriptionStrategy>(c => c.SubscribePlainMessages, true);
        }
    }
}