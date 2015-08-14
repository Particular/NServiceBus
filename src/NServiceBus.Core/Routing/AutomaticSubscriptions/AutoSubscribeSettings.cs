namespace NServiceBus.AutomaticSubscriptions.Config
{
    using NServiceBus.Features;

    /// <summary>
    /// Provides fine grained control over auto subscribe.
    /// </summary>
    public partial class AutoSubscribeSettings
    {
        BusConfiguration config;

        internal AutoSubscribeSettings(BusConfiguration config)
        {
            this.config = config;
        }

        /// <summary>
        /// Turns off auto subscriptions for sagas. Sagas where not auto subscribed by default before v4.
        /// </summary>
        public void DoNotAutoSubscribeSagas()
        {
            GetSettings().AutoSubscribeSagas = false;
        }

        /// <summary>
        /// Turns on auto-subscriptions for messages not marked as commands as only messages marked as events are included by default. 
        /// This was the default before v4.
        /// </summary>
        public void AutoSubscribePlainMessages()
        {
            GetSettings().SubscribePlainMessages = true;
        }


        AutoSubscribe.SubscribeSettings GetSettings()
        {
            AutoSubscribe.SubscribeSettings settings;

            if (!config.Settings.TryGet(out settings))
            {
                settings = new AutoSubscribe.SubscribeSettings();
                config.Settings.Set<AutoSubscribe.SubscribeSettings>(settings);
            }
            return settings;
        }

    }
}