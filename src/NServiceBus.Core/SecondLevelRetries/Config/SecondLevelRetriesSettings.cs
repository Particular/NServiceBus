namespace NServiceBus.SecondLevelRetries.Config
{
    using System;
    using Settings;

    public class SecondLevelRetriesSettings:ISetDefaultSettings
    {
        public SecondLevelRetriesSettings()
        {
            SettingsHolder.Instance.SetDefault("SecondLevelRetries.RetryPolicy", DefaultRetryPolicy.RetryPolicy);
        }

        public void CustomRetryPolicy(Func<TransportMessage, TimeSpan> customPolicy)
        {
            SettingsHolder.Instance.Set("SecondLevelRetries.RetryPolicy",customPolicy);
        }
    }
}