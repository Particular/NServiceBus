namespace NServiceBus.Routing
{
    /// <summary>
    /// Governs to which instances of a given endpoint a message is to be sent.
    /// </summary>
    public abstract class DistributionStrategy
    {
        /// <summary>
        /// Selects a destination instance for a command from all known instances of a logical endpoint.
        /// </summary>
        /// /// <param name="allInstances">All known endpoint instances belonging to the same logical endpoint.</param>
        /// <returns>The endpoint instance to receive the message.</returns>
        public abstract EndpointInstance SelectReceiver(EndpointInstance[] allInstances);

        /// <summary>
        /// Selects a subscriber address to receive an event from all known subscriber addresses of a logical endpoint.
        /// </summary>
        /// <param name="subscriberAddresses">All known subscriber addresses belonging to the same logical endpoint.</param>
        /// <returns>The subscriber address to receive the event.</returns>
        public abstract string SelectSubscriber(string[] subscriberAddresses);
    }
}