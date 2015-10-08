namespace NServiceBus.Transports
{
    /// <summary>
    /// The type of routing from the perspective of a transport.
    /// </summary>
    public enum OutboundRoutingType
    {
        /// <summary>
        /// Direct (use <see cref="ISendMessages"/>).
        /// </summary>
        DirectSend,
        /// <summary>
        /// Indirect (use <see cref="IPublishMessages"/>).
        /// </summary>
        IndirectPublish
    }
}