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
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        public static AutoSubscribeSettings AutoSubscribe(this EndpointConfiguration config)
        {
            Guard.AgainstNull(nameof(config), config);
            return new AutoSubscribeSettings(config);
        }
    }
}