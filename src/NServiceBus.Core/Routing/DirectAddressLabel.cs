namespace NServiceBus.Routing
{
    /// <summary>
    /// Represents a route directly to the specified destination.
    /// </summary>
    public class DirectAddressLabel : AddressLabel
    {
        /// <summary>
        /// The destination.
        /// </summary>
        public string Destination { get; }

        /// <summary>
        /// Initializes the strategy.
        /// </summary>
        /// <param name="destination">The destination.</param>
        public DirectAddressLabel(string destination)
        {
            Guard.AgainstNullAndEmpty("destination", destination);

            Destination = destination;
        }
    }
}