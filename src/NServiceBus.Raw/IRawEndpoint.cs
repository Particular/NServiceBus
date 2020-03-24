namespace NServiceBus.Raw
{
    using Settings;
    using Transport;

    /// <summary>
    /// Allows to send raw messages.
    /// </summary>
    public interface IRawEndpoint : IDispatchMessages
    {
        /// <summary>
        /// Translates a given logical address into a transport address.
        /// </summary>
        string ToTransportAddress(LogicalAddress logicalAddress);

        /// <summary>
        /// Returns the transport address of the endpoint.
        /// </summary>
        string TransportAddress { get; }

        /// <summary>
        /// Returns the logical name of the endpoint.
        /// </summary>
        string EndpointName { get; }

        /// <summary>
        /// Gets the settings used to initialize this endpoint instance.
        /// </summary>
        ReadOnlySettings Settings { get; }

        /// <summary>
        /// Gets the subscription manager if the underlying transport supports native publish-subscribe.
        /// </summary>
        IManageSubscriptions SubscriptionManager { get; }
    }
}