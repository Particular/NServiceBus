namespace NServiceBus.Routing
{
    /// <summary>
    /// Governs to which instances of a given endpoint a message is to be sent.
    /// </summary>
    public abstract class DistributionStrategy
    {
        /// <summary>
        /// Selects a destination instance for a message from all known addresses of a logical endpoint.
        /// </summary>
        public abstract string SelectReceiver(string[] receiverAddresses);
    }
}