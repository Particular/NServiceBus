namespace NServiceBus.SecondLevelRetries.Config
{
    using System;
    using Settings;

    public class SecondLevelRetriesSettings : IWantToRunBeforeConfiguration
    {
        public void Init(Configure configure)
        {
           configure.Settings.SetDefault("SecondLevelRetries.RetryPolicy", DefaultRetryPolicy.RetryPolicy);
        }

        public void CustomRetryPolicy(Func<TransportMessage, TimeSpan> customPolicy)
        {
            SettingsHolder.Instance.Set("SecondLevelRetries.RetryPolicy",customPolicy);
        }
    }
}