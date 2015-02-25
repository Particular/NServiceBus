namespace NServiceBus
{
    using System;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Features;
    using Transports;

    /// <summary>
    /// Transport definition for MSMQ
    /// </summary>
    public class MsmqTransport : TransportDefinition
    {
        /// <summary>
        /// Ctor
        /// </summary>
        public MsmqTransport()
        {
            RequireOutboxConsent = true;
            HasSupportForMultiQueueNativeTransactions = true;
        }

        /// <summary>
        /// Gives implementations access to the <see cref="BusConfiguration"/> instance at configuration time.
        /// </summary>
        protected internal override void Configure(BusConfiguration config)
        {
            // For MSMQ the endpoint differentiator is a no-op since you commonly scale out by running the same endpoint on a different machine.
            // if users want to run more than one instance on the same machine they need to set an explicit discriminator
            config.GetSettings()
                .SetDefault("EndpointInstanceDiscriminator", "");
               
            config.EnableFeature<MsmqTransportConfigurator>();
            config.EnableFeature<MessageDrivenSubscriptions>();
            config.EnableFeature<TimeoutManagerBasedDeferral>();

            config.Settings.EnableFeatureByDefault<StorageDrivenPublishing>();
            config.Settings.EnableFeatureByDefault<TimeoutManager>();
        }

        /// <summary>
        /// <see cref="TransportDefinition.GetSubScope"/>
        /// </summary>
        public override string GetSubScope(string address, string qualifier)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentNullException("address");
            }
            if (string.IsNullOrWhiteSpace(qualifier))
            {
                throw new ArgumentNullException("qualifier");
            }
            var split = address.Split('@');
            
            if (split.Length == 1)
            {
                return address + "." + qualifier;
            }
            if (split.Length == 2)
            {
                return string.Format("{0}.{1}@{2}", split[0], qualifier, split[1]);
            }
            var message = string.Format("Address contains multiple @ characters. Should be of the format 'queuename@machinename` or 'queuename`. Address supplied: '{0}'", address);
            throw new ArgumentException(message, "address");
        }
    }
}