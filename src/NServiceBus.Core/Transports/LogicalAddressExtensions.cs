namespace NServiceBus.Transport
{
    using Settings;

    /// <summary>
    /// Extension methods for access to various transport address helpers.
    /// </summary>
    public static class LogicalAddressExtensions
    {
        /// <summary>
        /// Gets the native transport address for the given local address.
        /// </summary>
        /// <returns>The native transport address.</returns>
        public static string GetTransportAddress(this ReadOnlySettings settings, LocalAddress localAddress)
        {
            return settings.Get<TransportInfrastructure>().ToTransportAddress(localAddress);
        }
    }
}