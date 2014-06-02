namespace NServiceBus.Features
{
    using System;
    using Config;
    using Faults.Forwarder;
    using NServiceBus.SecondLevelRetries;

    public class SecondLevelRetries : Feature
    {
        internal SecondLevelRetries()
        {
            EnableByDefault();
            Prerequisite(ShouldRun);
        }

        bool ShouldRun(FeatureConfigurationContext context)
        {
            // if we're not using the Fault Forwarder, we should act as if SLR is disabled
            //this will change when we make SLR a first class citizen
            if (!context.Container.HasComponent<FaultManager>())
            {
                return false;
            }
            var retriesConfig = context.Settings.GetConfigSection<SecondLevelRetriesConfig>();

            if (retriesConfig == null)
                return true;

            if (retriesConfig.NumberOfRetries == 0)
                return false;

            return retriesConfig.Enabled;
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var retriesConfig = context.Settings.GetConfigSection<SecondLevelRetriesConfig>();

            SetUpRetryPolicy(retriesConfig);

            var endpointName = context.Settings.Get<string>("EndpointName");


            var processorAddress = Address.Parse(endpointName).SubScope("Retries");

            var useRemoteRetryProcessor = context.Settings.HasSetting("SecondLevelRetries.AddressOfRetryProcessor");
            if (useRemoteRetryProcessor)
            {
                processorAddress = context.Settings.Get<Address>("SecondLevelRetries.AddressOfRetryProcessor");
            }

            context.Container.ConfigureProperty<FaultManager>(fm => fm.RetriesErrorQueue, processorAddress);
            context.Container.ConfigureProperty<SecondLevelRetriesProcessor>(rs => rs.InputAddress, processorAddress);
            context.Container.ConfigureProperty<SecondLevelRetriesProcessor>(rs => rs.RetryPolicy, context.Settings.GetOrDefault<Func<TransportMessage, TimeSpan>>("SecondLevelRetries.RetryPolicy") ?? DefaultRetryPolicy.RetryPolicy);
            context.Container.ConfigureProperty<SecondLevelRetriesProcessor>(rs => rs.Disabled, useRemoteRetryProcessor); 
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