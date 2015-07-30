namespace NServiceBus.Routing
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents a route directly to the specified destination.
    /// </summary>
    public class DirectToTargetDestination : RoutingStrategy
    {
        /// <summary>
        /// The destination.
        /// </summary>
        public string Destination { get; private set; }

        /// <summary>
        /// Initializes the strategy.
        /// </summary>
        /// <param name="destination">The destination.</param>
        public DirectToTargetDestination(string destination)
        {
            Guard.AgainstNullAndEmpty("destination", destination);

            Destination = destination;
        }

        /// <summary>
        /// Serializes the strategy to the supplied dictionary.
        /// </summary>
        /// <param name="options">The dictionary where the serialized data should be stored.</param> 
        public override void Serialize(Dictionary<string, string> options)
        {
            Guard.AgainstNull("options", options);

            options["Destination"] = Destination;
        }
    }
}