namespace NServiceBus
{
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
            config.EnableFeature<Features.MsmqTransport>();
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
            return Address.Parse(address).SubScope(qualifier).ToString();
        }
    }
}