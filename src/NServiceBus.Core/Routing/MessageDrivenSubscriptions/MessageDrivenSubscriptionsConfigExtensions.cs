namespace NServiceBus.Routing //So that it is not always visible.
{
    /// <summary>
    /// Provides extensions for configuring message driven subscriptions.
    /// </summary>
    public static class MessageDrivenSubscriptionsConfigExtensions
    {
        /// <summary>
        /// Forces this endpoint to use a legacy message driven subscription mode which makes it compatible with V5 and previous versions.
        /// </summary>
        /// <remarks>
        /// When scaling out with queue per endpoint instance the legacy mode should be used only in a single instance. Enabling the legacy
        /// mode in multiple instances will result in message duplication.
        /// </remarks>
        public static void UseLegacyMessageDrivenSubscriptionMode(this BusConfiguration busConfiguration)
        {
            busConfiguration.Settings.Set("NServiceBus.Routing.UseLegacyMessageDrivenSubscriptionMode", true);
        }
    }
}