namespace NServiceBus
{
    using AutomaticSubscriptions.Config;

    /// <summary>
    /// Adds support for custom configuration of the auto subscribe feature
    /// </summary>
    public static class AutoSubscribeSettingsExtensions
    {
        /// <summary>
        /// Use this method to change how auto subscribe works
        /// </summary>
        public static AutoSubscribeSettings AutoSubscribe(this ConfigurationBuilder config)
        {
            return new AutoSubscribeSettings(config);
        }
    }
}