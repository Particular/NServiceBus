namespace NServiceBus.AutomaticSubscriptions.Config
{
    using Settings;

    public class AutoSubscribeSettings:ISetDefaultSettings
    {
        /// <summary>
        /// Turns off auto subscriptions for sagas. Sagas where not auto subscribed by default before v4
        /// </summary>
        public AutoSubscribeSettings DoNotAutoSubscribeSagas()
        {
            SettingsHolder.Instance.SetProperty<AutoSubscriptionStrategy>(c => c.DoNotAutoSubscribeSagas, true);
            return this;
        }

        /// <summary>
        /// Allows to endpoint to subscribe to messages owned by the local endpoint
        /// </summary>
        public AutoSubscribeSettings DoNotRequireExplicitRouting()
        {
            SettingsHolder.Instance.SetProperty<AutoSubscriptionStrategy>(c => c.DoNotRequireExplicitRouting, true); 
            return this;
        }

        /// <summary>
        /// Turns on auto-subscriptions for messages not marked as commands. This was the default before v4
        /// </summary>
        public AutoSubscribeSettings AutoSubscribePlainMessages()
        {
            SettingsHolder.Instance.SetProperty<AutoSubscriptionStrategy>(c => c.SubscribePlainMessages, true);
            return this;
        }
    }
}