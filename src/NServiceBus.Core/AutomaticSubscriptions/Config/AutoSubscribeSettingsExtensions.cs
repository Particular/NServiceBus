namespace NServiceBus
{
    using System;
    using AutomaticSubscriptions.Config;
    using Features;


    public static class AutoSubscribeSettingsExtensions
    {
        public static FeatureSettings AutoSubscribe(this FeatureSettings settings, Action<AutoSubscribeSettings> customSettings)
        {
            customSettings(new AutoSubscribeSettings());

            return settings;
        }
    }
}