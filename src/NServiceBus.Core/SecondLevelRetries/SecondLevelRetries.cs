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
            DependsOn<ForwarderFaultManager>();

            Prerequisite(context => !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"), "Send only endpoints can't use SLR since it requires receive capabilities");

            Prerequisite(IsEnabledInConfig, "SLR was disabled in config");
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var processorAddress = context.Settings.LocalAddress().SubScope("Retries");
            var useRemoteRetryProcessor = context.Settings.HasSetting("SecondLevelRetries.AddressOfRetryProcessor");
            
            if (useRemoteRetryProcessor)
            {
                processorAddress = context.Settings.Get<Address>("SecondLevelRetries.AddressOfRetryProcessor");
            }

            var container = context.Container;
            container.ConfigureProperty<FaultManager>(fm => fm.RetriesErrorQueue, processorAddress);
            container.ConfigureProperty<SecondLevelRetriesProcessor>(rs => rs.InputAddress, processorAddress);
            var retryPolicy = context.Settings.GetOrDefault<Func<TransportMessage, TimeSpan>>("SecondLevelRetries.RetryPolicy");
            if (retryPolicy != null)
            {
                container.ConfigureProperty<SecondLevelRetriesProcessor>(rs => rs.RetryPolicy, retryPolicy);
            }
            container.ConfigureProperty<SecondLevelRetriesProcessor>(rs => rs.Disabled, useRemoteRetryProcessor); 
    
            var retriesConfig = context.Settings.GetConfigSection<SecondLevelRetriesConfig>();
            if (retriesConfig == null)
                return;

            container.ConfigureProperty<SecondLevelRetriesProcessor>(rs => rs.NumberOfRetries, retriesConfig.NumberOfRetries); 

            if (retriesConfig.TimeIncrease != TimeSpan.MinValue)
            {
                container.ConfigureProperty<SecondLevelRetriesProcessor>(rs => rs.TimeIncrease, retriesConfig.TimeIncrease); 
            }
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

    }
}