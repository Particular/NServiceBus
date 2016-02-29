namespace NServiceBus.Transports.Msmq
{
    using NServiceBus.Settings;

    /// <summary>
    /// Defines the subscription store for MSMQ transport.
    /// </summary>
    public abstract class SubscriptionStoreDefinition
    {
        /// <summary>
        /// Initializes the definition.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <returns>Subscription store infrastructure.</returns>
        protected internal abstract SubscriptionStoreInfrastructure Initialize(SettingsHolder settings);
    }
}