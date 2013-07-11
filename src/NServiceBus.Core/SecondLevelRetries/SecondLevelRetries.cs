namespace NServiceBus.Management.Retries
{
    using System;
    using Settings;

    public class SecondLevelRetries
    {
        [ObsoleteEx(RemoveInVersion = "5.0", TreatAsErrorFromVersion = "4.0", Replacement = "Configure.Features.SecondLevelRetries(s => s.CustomRetryPolicy())")]
        public static Func<TransportMessage, TimeSpan> RetryPolicy
        {
            get { return SettingsHolder.Get("SecondLevelRetries.RetryPolicy") as Func<TransportMessage, TimeSpan>; }
            set { SettingsHolder.Set("SecondLevelRetries.RetryPolicy", value); }
        }
    }
}

namespace NServiceBus.Features
{
    using System;
    using Config;
    using Faults.Forwarder;
    using NServiceBus.SecondLevelRetries;
    using Settings;

    

    public class SecondLevelRetries : Feature
    {
        public override bool ShouldBeEnabled()
        {
            // if we're not using the Fault Forwarder, we should act as if SLR is disabled
            //this will change when we make SLR a first class citizen
            if (!Configure.Instance.Configurer.HasComponent<FaultManager>())
            {
                return false;
            }
            var retriesConfig = Configure.GetConfigSection<SecondLevelRetriesConfig>();

            if (retriesConfig == null)
                return true;

            if (retriesConfig.NumberOfRetries == 0)
                return false;

            return retriesConfig.Enabled;
        }

        public override bool IsEnabledByDefault
        {
            get
            {
                return true;
            }
        }

        public override void Initialize()
        {
            var retriesConfig = Configure.GetConfigSection<SecondLevelRetriesConfig>();

            SetUpRetryPolicy(retriesConfig);

            var processorAddress = Address.Parse(Configure.EndpointName).SubScope("Retries");

            var useRemoteRetryProcessor = SettingsHolder.HasSetting("SecondLevelRetries.AddressOfRetryProcessor");
            if (useRemoteRetryProcessor)
            {
                processorAddress = SettingsHolder.Get<Address>("SecondLevelRetries.AddressOfRetryProcessor");
            }

            Configure.Instance.Configurer.ConfigureProperty<FaultManager>(fm => fm.RetriesErrorQueue, processorAddress);
            Configure.Instance.Configurer.ConfigureProperty<SecondLevelRetriesProcessor>(rs => rs.InputAddress, processorAddress);
            Configure.Instance.Configurer.ConfigureProperty<SecondLevelRetriesProcessor>(rs => rs.RetryPolicy, SettingsHolder.Get<Func<TransportMessage,TimeSpan>>("SecondLevelRetries.RetryPolicy"));

            if (useRemoteRetryProcessor)
            {
                Configure.Instance.Configurer.ConfigureProperty<SecondLevelRetriesProcessor>(rs => rs.Disabled, true);
            }
        }

        static void SetUpRetryPolicy(SecondLevelRetriesConfig retriesConfig)
        {
            if (retriesConfig == null)
                return;

            DefaultRetryPolicy.NumberOfRetries = retriesConfig.NumberOfRetries;

            if (retriesConfig.TimeIncrease != TimeSpan.MinValue)
            {
                DefaultRetryPolicy.TimeIncrease = retriesConfig.TimeIncrease;
            }
        }
    }
}