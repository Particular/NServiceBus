namespace NServiceBus
{
    using NServiceBus.Features;
    using Transports;

    /// <summary>
    /// Transport definition for MSMQ
    /// </summary>
    public class Msmq : TransportDefinition
    {
        /// <summary>
        /// Ctor
        /// </summary>
        public Msmq()
        {
            RequireOutboxConsent = true;
        }

        /// <summary>
        /// Gives implementations access to the <see cref="ConfigurationBuilder"/> instance at configuration time.
        /// </summary>
        protected internal override void Configure(ConfigurationBuilder config)
        {
            config.EnableFeature<MsmqTransport>();
            config.EnableFeature<MessageDrivenSubscriptions>();
            config.EnableFeature<TimeoutManagerBasedDeferral>();

            config.settings.EnableFeatureByDefault<StorageDrivenPublishing>();
            config.settings.EnableFeatureByDefault<TimeoutManager>();
        }
    }
}