namespace NServiceBus
{
    using System;
    using Config;
    using Features;

    class DetectObsoleteConfigurationSettings : Feature
    {
        public DetectObsoleteConfigurationSettings()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var unicastBusConfig = context.Settings.GetConfigSection<UnicastBusConfig>();

            if (!string.IsNullOrWhiteSpace(unicastBusConfig?.ForwardReceivedMessagesTo))
            {
                throw new NotSupportedException($"The {nameof(UnicastBusConfig.ForwardReceivedMessagesTo)} attribute in the {nameof(UnicastBusConfig)} configuration section is no longer supported. Switch to the code API by using `{nameof(EndpointConfiguration)}.ForwardReceivedMessagesTo` instead.");
            }

            if (!string.IsNullOrWhiteSpace(unicastBusConfig?.DistributorControlAddress))
            {
                throw new NotSupportedException($"The {nameof(UnicastBusConfig.DistributorControlAddress)} attribute in the {nameof(UnicastBusConfig)} configuration section is no longer supported. Remove this from the configuration section. Switch to the code API by using `{nameof(EndpointConfiguration)}.EnlistWithLegacyMSMQDistributor` instead.");
            }

            if (!string.IsNullOrWhiteSpace(unicastBusConfig?.DistributorDataAddress))
            {
                throw new NotSupportedException($"The {nameof(UnicastBusConfig.DistributorDataAddress)} attribute in the {nameof(UnicastBusConfig)} configuration section is no longer supported. Remove this from the configuration section. Switch to the code API by using `{nameof(EndpointConfiguration)}.EnlistWithLegacyMSMQDistributor` instead.");
            }

            var masterNodeConfig = context.Settings.GetConfigSection<MasterNodeConfig>();

            if (masterNodeConfig != null)
            {
                throw new NotSupportedException($"The {nameof(MasterNodeConfig)} configuration section is no longer supported. Remove this from this configuration section. Switch to the code API by using `{nameof(EndpointConfiguration)}.EnlistWithLegacyMSMQDistributor` instead.");
            }

            var secondLevelRetriesConfig = context.Settings.GetConfigSection<SecondLevelRetriesConfig>();

            if (secondLevelRetriesConfig != null)
            {
                throw new NotSupportedException($"The {nameof(SecondLevelRetriesConfig)} configuration section is no longer supported. Remove this from this configuration section. Switch to the code API by using `endpointConfiguration.Recoverability().Delayed(settings => ...)` instead.");
            }

            var transportConfig = context.Settings.GetConfigSection<TransportConfig>();

            if (transportConfig != null)
            {
                throw new NotSupportedException($"The {nameof(TransportConfig)} configuration section is no longer supported. Remove this from this configuration section. Switch to the code API by using `endpointConfiguration.LimitMessageProcessingConcurrencyTo(1)` to change the concurrency level or `endpointConfiguration.Recoverability().Immediate(settings => settings.NumberOfRetries(5)` to change the number of immediate retries instead.");
            }
        }
    }
}