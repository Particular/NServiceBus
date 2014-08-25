namespace NServiceBus.Features
{
    using System;
    using Config;
    using Faults.Forwarder;
    using NServiceBus.SecondLevelRetries;
    using NServiceBus.Transports;

    /// <summary>
    /// Used to configure Second Level Retries.
    /// </summary>
    public class SecondLevelRetries : Feature
    {
        internal SecondLevelRetries()
        {
            EnableByDefault();
            DependsOn<ForwarderFaultManager>();

            Prerequisite(IsEnabledInConfig, "SLR was disabled in config");
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            if (context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"))
            {
                return;
            }

            var retriesConfig = context.Settings.GetConfigSection<SecondLevelRetriesConfig>();

            SetUpRetryPolicy(retriesConfig);


            var selectedTransportDefinition = context.Settings.Get<TransportDefinition>();

            var processorAddress = selectedTransportDefinition.GetSubScope(context.Settings.EndpointName(), "Retries");

            var useRemoteRetryProcessor = context.Settings.HasSetting("SecondLevelRetries.AddressOfRetryProcessor");
            if (useRemoteRetryProcessor)
            {
                processorAddress = context.Settings.Get<string>("SecondLevelRetries.AddressOfRetryProcessor");
            }

            context.Container.ConfigureProperty<FaultManager>(fm => fm.RetriesErrorQueue, processorAddress);
            context.Container.ConfigureProperty<SecondLevelRetriesProcessor>(rs => rs.InputAddress, processorAddress);
            context.Container.ConfigureProperty<SecondLevelRetriesProcessor>(rs => rs.RetryPolicy, context.Settings.GetOrDefault<Func<TransportMessage, TimeSpan>>("SecondLevelRetries.RetryPolicy") ?? DefaultRetryPolicy.RetryPolicy);
            context.Container.ConfigureProperty<SecondLevelRetriesProcessor>(rs => rs.Disabled, useRemoteRetryProcessor); 
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