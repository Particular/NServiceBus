namespace NServiceBus
{
    using Transport;

    /// <summary>
    /// Enables translation of logical queue address into transport-specific address strings.
    /// </summary>
    public interface ITransportAddressResolver
    {
        /// <summary>
        /// Translates a <see cref="QueueAddress"/> object into a transport specific queue address-string.
        /// </summary>
        string ToTransportAddress(QueueAddress queueAddress);
    }
}