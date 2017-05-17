namespace NServiceBus
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
            DetectObsoleteConfiguration(context.Settings.GetConfigSection<Config.Logging>());
            DetectObsoleteConfiguration(context.Settings.GetConfigSection<AuditConfig>());
            DetectObsoleteConfiguration(context.Settings.GetConfigSection<MessageForwardingInCaseOfFaultConfig>());
            DetectObsoleteConfiguration(context.Settings.GetConfigSection<MsmqSubscriptionStorageConfig>());
        }

        static void DetectObsoleteConfiguration(UnicastBusConfig unicastBusConfig)
        {
            if (unicastBusConfig?.TimeToBeReceivedOnForwardedMessages > TimeSpan.Zero)
            {
                Logger.Error($"The use of the {nameof(UnicastBusConfig.TimeToBeReceivedOnForwardedMessages)} attribute in the {nameof(UnicastBusConfig)} configuration section is discouraged and will be removed in the next major version.");
            }

            if (!string.IsNullOrWhiteSpace(unicastBusConfig?.TimeoutManagerAddress))
            {
                Logger.Error($"The use of the {nameof(UnicastBusConfig.TimeoutManagerAddress)} attribute in the {nameof(UnicastBusConfig)} configuration section is discouraged and will be removed in the next major version. Switch to the code API by using  '{nameof(EndpointConfiguration)}.UseExternalTimeoutManager' instead.");
            }

            if (unicastBusConfig?.MessageEndpointMappings != null)
            {
                Logger.Error($"The use of the {nameof(UnicastBusConfig.MessageEndpointMappings)} in the {nameof(UnicastBusConfig)} configuration section is discouraged and will be removed in the next major version. Switch to the code API by using  '{nameof(EndpointConfiguration)}.UseTransport<T>().Routing()' instead.");
            }
        }

        static void DetectObsoleteConfiguration(AuditConfig auditConfig)
        {
            if (!string.IsNullOrWhiteSpace(auditConfig?.QueueName))
            {
                Logger.Error($"The use of the {nameof(AuditConfig.QueueName)} attribute in the {nameof(AuditConfig)} configuration section is discouraged and will be removed in the next major version. Switch to the code API by using '{nameof(EndpointConfiguration)}.AuditProcessedMessagesTo' instead.");
            }

            if (auditConfig?.OverrideTimeToBeReceived != null)
            {
                Logger.Error($"The use of the {nameof(AuditConfig.OverrideTimeToBeReceived)} attribute in the {nameof(AuditConfig)} configuration section is discouraged and will be removed in the next major version. Switch to the code API by using '{nameof(EndpointConfiguration)}.AuditProcessedMessagesTo' instead.");
            }
        }

        static void DetectObsoleteConfiguration(Config.Logging loggingConfig)
        {
            if (loggingConfig != null)
            {
                Logger.Error("Usage of the 'NServiceBus.Config.Logging' configuration section is discouraged and will be removed with the next major version. Use the LogManager.Use<DefaultFactory>() code configuration API instead.");
            }
        }

        static void DetectObsoleteConfiguration(MessageForwardingInCaseOfFaultConfig faultConfig)
        {
            if (faultConfig != null)
            {
                Logger.Error("Usage of the 'NServiceBus.Config.MessageForwardingInCaseOfFaultConfig' configuration section is discouraged and will be removed with the next major version. Use the 'endpointConfiguration.SendFailedMessagesTo()' code configuration API instead.");
            }
        }

        static void DetectObsoleteConfiguration(MsmqSubscriptionStorageConfig msmqSubscriptionStorageConfig)
        {
            if (msmqSubscriptionStorageConfig != null)
            {
                Logger.Error("Usage of the 'NServiceBus.Config.MsmqSubscriptionStorageConfig' configuration section is discouraged and will be removed with the next major version. Use the 'endpointConfiguration.UsePersistence<MsmqPersistence>().SubscriptionQueue()' code configuration API instead.");
            }
        }

        static ILog Logger = LogManager.GetLogger<DetectObsoleteConfigurationSettings>();
    }
}