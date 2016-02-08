namespace NServiceBus
{
    using System;
    using NServiceBus.Config;
    using NServiceBus.Features;

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
                throw new NotSupportedException($"The {nameof(UnicastBusConfig.ForwardReceivedMessagesTo)} attribute in the {nameof(UnicastBusConfig)} configuration section is no longer supported. Please switch to the code first API by using `{nameof(EndpointConfiguration)}.ForwardReceivedMessagesTo` instead.");
            }
        }
    }

    
}