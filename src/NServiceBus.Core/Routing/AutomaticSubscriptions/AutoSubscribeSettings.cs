namespace NServiceBus.AutomaticSubscriptions.Config
{
    using NServiceBus.Features;

    /// <summary>
    /// Provides fine grained control over auto subscribe.
    /// </summary>
    public partial class AutoSubscribeSettings
    {
        EndpointConfiguration config;

        internal AutoSubscribeSettings(EndpointConfiguration config)
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