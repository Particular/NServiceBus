namespace NServiceBus
{
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
    }
}