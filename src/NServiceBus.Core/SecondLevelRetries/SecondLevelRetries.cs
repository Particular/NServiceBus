namespace NServiceBus.Features
{
    using System;
    using Config;
    using Faults.Forwarder;
    using NServiceBus.SecondLevelRetries;

    /// <summary>
    /// Used to configure Second Level Retries.
    /// </summary>
    public class SecondLevelRetries : Feature
    {
        internal SecondLevelRetries()
        {
            EnableByDefault();
           
            // if we're not using the Fault Forwarder, we should act as if SLR is disabled
            //this will change when we make SLR a first class citizen
            Prerequisite(c => c.Container.HasComponent<FaultManager>(), "A custom faultmanager was defined");
            Prerequisite(IsEnabledInConfig, "SLR was disabled in config");
        }

        bool IsEnabledInConfig(FeatureConfigurationContext context)
        {
            var retriesConfig = context.Settings.GetConfigSection<SecondLevelRetriesConfig>();

            if (retriesConfig == null)
                return true;

            if (retriesConfig.NumberOfRetries == 0)
                return false;

            return retriesConfig.Enabled;
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
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