namespace NServiceBus
{
    using AutomaticSubscriptions.Config;

    /// <summary>
    /// Adds support for custom configuration of the auto subscribe feature.
    /// </summary>
    public static class AutoSubscribeSettingsExtensions
    {
        /// <summary>
        /// Use this method to change how auto subscribe works.
        /// </summary>
        /// <param name="config">The <see cref="BusConfiguration"/> instance to apply the settings to.</param>
        public static AutoSubscribeSettings AutoSubscribe(this BusConfiguration config)
        {
            Guard.AgainstNull(config, "config");
            return new AutoSubscribeSettings(config);
        }
    }
}