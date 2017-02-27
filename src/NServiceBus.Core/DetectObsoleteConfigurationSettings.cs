﻿namespace NServiceBus
{
    using System;
    using Config;
    using Features;
    using Logging;

    class DetectObsoleteConfigurationSettings : Feature
    {
        public DetectObsoleteConfigurationSettings()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            DetectObsoleteConfiguration(context.Settings.GetConfigSection<UnicastBusConfig>());
            DetectObsoleteConfiguration(context.Settings.GetConfigSection<MasterNodeConfig>());
            DetectObsoleteConfiguration(context.Settings.GetConfigSection<SecondLevelRetriesConfig>());
            DetectObsoleteConfiguration(context.Settings.GetConfigSection<TransportConfig>());
        }

        static void DetectObsoleteConfiguration(UnicastBusConfig unicastBusConfig)
        {
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

            if (unicastBusConfig?.TimeToBeReceivedOnForwardedMessages != null)
            {
                Logger.Warn($"The use of the {nameof(UnicastBusConfig.TimeToBeReceivedOnForwardedMessages)} attribute in the {nameof(UnicastBusConfig)} configuration section is discouraged and will be removed in the next major version.");
            }
        }

        static void DetectObsoleteConfiguration(MasterNodeConfig masterNodeConfig)
        {
            if (masterNodeConfig != null)
            {
                throw new NotSupportedException($"The {nameof(MasterNodeConfig)} configuration section is no longer supported. Remove this from this configuration section. Switch to the code API by using `{nameof(EndpointConfiguration)}.EnlistWithLegacyMSMQDistributor` instead.");
            }
        }

        static void DetectObsoleteConfiguration(SecondLevelRetriesConfig secondLevelRetriesConfig)
        {
            if (secondLevelRetriesConfig != null)
            {
                throw new NotSupportedException($"The {nameof(SecondLevelRetriesConfig)} configuration section is no longer supported. Remove this from this configuration section. Switch to the code API by using `endpointConfiguration.Recoverability().Delayed(settings => ...)` instead.");
            }
        }

        static void DetectObsoleteConfiguration(TransportConfig transportConfig)
        {
            if (transportConfig != null)
            {
                throw new NotSupportedException($"The {nameof(TransportConfig)} configuration section is no longer supported. Remove this from this configuration section. Switch to the code API by using `endpointConfiguration.LimitMessageProcessingConcurrencyTo(1)` to change the concurrency level or `endpointConfiguration.Recoverability().Immediate(settings => settings.NumberOfRetries(5)` to change the number of immediate retries instead.");
            }
        }

        static ILog Logger = LogManager.GetLogger<DetectObsoleteConfigurationSettings>();
    }
}