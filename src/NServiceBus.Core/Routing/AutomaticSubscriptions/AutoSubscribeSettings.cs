namespace NServiceBus.AutomaticSubscriptions.Config
{
    using System;
    using Features;

    /// <summary>
    /// Provides fine grained control over auto subscribe.
    /// </summary>
    public class AutoSubscribeSettings
    {
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

        /// <summary>
        /// Configure AutoSubscribe to not subscribe automatically to the given event type.
        /// </summary>
        public AutoSubscribeSettings DisableFor<T>()
        {
            return DisableFor(typeof(T));
        }

        /// <summary>
        /// Configure AutoSubscribe to not subscribe automatically to the given event type.
        /// </summary>
        public AutoSubscribeSettings DisableFor(Type eventType)
        {
            Guard.AgainstNull(nameof(eventType), eventType);

            GetSettings().ExcludedTypes.Add(eventType);
            return this;
        }

        AutoSubscribe.SubscribeSettings GetSettings()
        {
            if (!config.Settings.TryGet(out AutoSubscribe.SubscribeSettings settings))
            {
                settings = new AutoSubscribe.SubscribeSettings();
                config.Settings.Set<AutoSubscribe.SubscribeSettings>(settings);
            }
            return settings;
        }

        EndpointConfiguration config;
    }
}