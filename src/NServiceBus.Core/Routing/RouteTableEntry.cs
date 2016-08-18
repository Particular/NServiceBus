namespace NServiceBus.Routing
{
    using System;

    /// <summary>
    /// Represents an entry in a routing table.
    /// </summary>
    public class RouteTableEntry
    {
        /// <summary>
        /// Type of message.
        /// </summary>
        public Type MessageType { get; }
        /// <summary>
        /// Route for the message type.
        /// </summary>
        public UnicastRoute Route { get; }

        /// <summary>
        /// Creates a new entry.
        /// </summary>
        public RouteTableEntry(Type messageType, UnicastRoute route)
        {
            this.MessageType = messageType;
            this.Route = route;
        }
    }
}