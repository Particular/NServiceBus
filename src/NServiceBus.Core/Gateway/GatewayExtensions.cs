namespace NServiceBus.Gateway
{
    using HeaderManagement;

    /// <summary>
    ///     extensions internal to the gateway
    /// </summary>
    internal static class GatewayExtensions
    {
        /// <summary>
        ///     legacy mode support
        /// </summary>
        /// <returns>
        ///     true when message received from gateway other than v4
        ///     or v4 site is configured to forward messages using legacy mode,
        ///     false otherwise
        /// </returns>
        public static bool IsLegacyGatewayMessage(this TransportMessage message)
        {
            var legacyMode = true;

            // Gateway v3 would never have sent this header
            string legacyModeString;
            if (message.Headers.TryGetValue(GatewayHeaders.LegacyMode, out legacyModeString))
            {
                bool.TryParse(legacyModeString, out legacyMode);
            }

            return legacyMode;
        }
    }
}