namespace NServiceBus.Transports.Msmq
{
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Settings;


    /// <summary>
    /// Allows to customize settings of subscription store.
    /// </summary>
    public class SubscriptionStoreSettings<T> : ExposeSettings
        where T : SubscriptionStoreDefinition
    {
        internal SubscriptionStoreSettings(SettingsHolder settings)
            : base(settings)
        {
        }
    }
}