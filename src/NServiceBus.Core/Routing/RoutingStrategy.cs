namespace NServiceBus.Routing
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents different ways how the transport should route a given message.
    /// </summary>
    public abstract class RoutingStrategy
    {
        /// <summary>
        /// Serializes the strategy to the supplied dictionary.
        /// </summary>
        /// <param name="options">The dictionary where the serialized data should be stored.</param>
        public abstract void Serialize(Dictionary<string, string> options);
    }
}