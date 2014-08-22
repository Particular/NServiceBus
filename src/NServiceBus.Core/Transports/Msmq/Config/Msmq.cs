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
        /// Gives implementations access to the <see cref="BusConfiguration"/> instance at configuration time.
        /// </summary>
        protected internal override void Configure(BusConfiguration config)
        {
            config.EnableFeature<MsmqTransport>();
            config.EnableFeature<MessageDrivenSubscriptions>();
            config.EnableFeature<TimeoutManagerBasedDeferral>();

            config.Settings.EnableFeatureByDefault<StorageDrivenPublishing>();
            config.Settings.EnableFeatureByDefault<TimeoutManager>();
        }
    }
}