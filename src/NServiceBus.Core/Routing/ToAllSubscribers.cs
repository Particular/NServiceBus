namespace NServiceBus.Routing
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a route that should deliver the message to all interested subscribers.
    /// </summary>
    public class ToAllSubscribers:RoutingStrategy
    {
        /// <summary>
        /// Initializes a new insatnce of <see cref="ToAllSubscribers"/>.
        /// </summary>
        /// <param name="eventType">The event being published.</param>
        public ToAllSubscribers(Type eventType)
        {
            EventType = eventType;
        }

        /// <summary>
        /// The event being published.
        /// </summary>
        public Type EventType { get; private set; }

        /// <summary>
        /// Serializes the strategy to the supplied dictionary.
        /// </summary>
        /// <param name="options">The dictionary where the serialized data should be stored.</param>
        public override void Serialize(Dictionary<string, string> options)
        {
            options["EventType"] = EventType.AssemblyQualifiedName;
        }
    }
}