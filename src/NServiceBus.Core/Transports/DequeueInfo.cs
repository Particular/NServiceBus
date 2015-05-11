namespace NServiceBus.Transports
{
    /// <summary>
    /// Contains the return information on how the message pump has been set up.
    /// </summary>
    public class DequeueInfo
    {
        /// <summary>
        /// An address other endpoints should use to sent messages to this endpoint.
        /// </summary>
        public readonly string PublicAddress;

        /// <summary>
        /// Creates new instance.
        /// </summary>
        /// <param name="publicAddress">An address other endpoints should use to sent messages to this endpoint.</param>
        public DequeueInfo(string publicAddress)
        {
            PublicAddress = publicAddress;
        }
    }
}