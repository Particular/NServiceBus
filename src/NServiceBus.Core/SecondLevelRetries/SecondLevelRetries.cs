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
            var retryPolicy = context.Settings.GetOrDefault<Func<TransportMessage, TimeSpan>>("SecondLevelRetries.RetryPolicy");

            var secondLevelRetriesConfiguration = new SecondLevelRetriesConfiguration();
            if (retryPolicy != null)
            {
                secondLevelRetriesConfiguration.RetryPolicy = retryPolicy;
            }

            container.ConfigureProperty<FaultManager>(fm => fm.RetriesErrorQueue, processorAddress)
                .ConfigureProperty<FaultManager>(fm => fm.SecondLevelRetriesConfiguration, secondLevelRetriesConfiguration);

            container.ConfigureProperty<SecondLevelRetriesProcessor>(p => p.InputAddress, processorAddress)
                .ConfigureProperty<SecondLevelRetriesProcessor>(p => p.SecondLevelRetriesConfiguration, secondLevelRetriesConfiguration)
                .ConfigureProperty<SecondLevelRetriesProcessor>(p => p.Disabled, useRemoteRetryProcessor);

            var retriesConfig = context.Settings.GetConfigSection<SecondLevelRetriesConfig>();
            if (retriesConfig == null)
            {
                return;
            }

            secondLevelRetriesConfiguration.NumberOfRetries = retriesConfig.NumberOfRetries;

            if (retriesConfig.TimeIncrease != TimeSpan.MinValue)
            {
                secondLevelRetriesConfiguration.TimeIncrease = retriesConfig.TimeIncrease;
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