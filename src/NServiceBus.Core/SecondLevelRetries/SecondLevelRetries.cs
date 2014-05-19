namespace NServiceBus.Features
{
    using System;
    using Config;
    using Faults.Forwarder;
    using NServiceBus.SecondLevelRetries;

    public class SecondLevelRetries : Feature
    {
        public override bool ShouldBeEnabled(Configure config)
        {
            // if we're not using the Fault Forwarder, we should act as if SLR is disabled
            //this will change when we make SLR a first class citizen
            if (!config.Configurer.HasComponent<FaultManager>())
            {
                return false;
            }
            var retriesConfig = config.GetConfigSection<SecondLevelRetriesConfig>();

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

        public override void Initialize(Configure config)
        {
            var retriesConfig = config.GetConfigSection<SecondLevelRetriesConfig>();

            SetUpRetryPolicy(retriesConfig);

            var processorAddress = Address.Parse(config.EndpointName).SubScope("Retries");

            var useRemoteRetryProcessor = config.Settings.HasSetting("SecondLevelRetries.AddressOfRetryProcessor");
            if (useRemoteRetryProcessor)
            {
                processorAddress = config.Settings.Get<Address>("SecondLevelRetries.AddressOfRetryProcessor");
            }

            config.Configurer.ConfigureProperty<FaultManager>(fm => fm.RetriesErrorQueue, processorAddress);
            config.Configurer.ConfigureProperty<SecondLevelRetriesProcessor>(rs => rs.InputAddress, processorAddress);
            config.Configurer.ConfigureProperty<SecondLevelRetriesProcessor>(rs => rs.RetryPolicy, config.Settings.Get<Func<TransportMessage, TimeSpan>>("SecondLevelRetries.RetryPolicy"));

            if (useRemoteRetryProcessor)
            {
                config.Configurer.ConfigureProperty<SecondLevelRetriesProcessor>(rs => rs.Disabled, true);
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