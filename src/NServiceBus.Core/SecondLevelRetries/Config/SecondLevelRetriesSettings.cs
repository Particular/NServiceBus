namespace NServiceBus.SecondLevelRetries.Config
{
    using System;
    using NServiceBus.Settings;

    public class SecondLevelRetriesSettings:ISetDefaultSettings
    {
        public SecondLevelRetriesSettings()
        {
            SettingsHolder.SetDefault("SecondLevelRetries.RetryPolicy", DefaultRetryPolicy.RetryPolicy);
        }

        public void CustomRetryPolicy(Func<TransportMessage, TimeSpan> customPolicy)
        {
            SettingsHolder.Set("SecondLevelRetries.RetryPolicy",customPolicy);
        }
    }
}