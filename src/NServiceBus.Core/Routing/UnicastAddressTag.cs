namespace NServiceBus.Routing
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents a route directly to the specified destination.
    /// </summary>
    public class UnicastAddressTag : AddressTag
    {
        /// <summary>
        /// The destination.
        /// </summary>
        public string Destination { get; }

        /// <summary>
        /// Initializes the strategy.
        /// </summary>
        /// <param name="destination">The destination.</param>
        /// <param name="extensionData">The additional data about the destination.</param>
        public UnicastAddressTag(string destination, Dictionary<string, string> extensionData)
            : base(extensionData)
        {
            Guard.AgainstNullAndEmpty(nameof(destination), destination);
            Destination = destination;
        }
    }
}