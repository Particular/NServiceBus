namespace NServiceBus
{
    using System;
    using AutomaticSubscriptions.Config;

    /// <summary>
    /// Adds support for custom configuration of the auto subscribe feature
    /// </summary>
    public static class AutoSubscribeSettingsExtensions
    {
        /// <summary>
        /// Use this method to change how auto subscribe works
        /// </summary>
        /// <param name="config"></param>
        /// <param name="customSettings"></param>
        /// <returns></returns>
        public static Configure AutoSubscribe(this Configure config, Action<AutoSubscribeSettings> customSettings)
        {
            customSettings(new AutoSubscribeSettings(config));

            return config;
        }
    }
}