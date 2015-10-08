namespace NServiceBus
{
    using NServiceBus.Routing.MessageDrivenSubscriptions;

    /// <summary>
    /// Allows to configure publishers.
    /// </summary>
    public static class PublishersSettingsExtensions
    {
        /// <summary>
        /// Gets the publisher settings.
        /// </summary>
        public static Publishers Pubishers(this BusConfiguration config)
        {
            Guard.AgainstNull("config", config);
            Publishers publishers;
            if (!config.Settings.TryGet(out publishers))
            {
                publishers = new Publishers();
                config.Settings.Set<Publishers>(new Publishers());
            }
            return publishers;
        }
    }
}