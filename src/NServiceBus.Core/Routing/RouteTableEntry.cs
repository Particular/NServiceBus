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

        bool Equals(RouteTableEntry other)
        {
            return Equals(MessageType, other.MessageType) && Equals(Route, other.Route);
        }

        /// <summary>Determines whether the specified object is equal to the current object.</summary>
        /// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
        /// <param name="obj">The object to compare with the current object. </param>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((RouteTableEntry) obj);
        }

        /// <summary>Serves as the default hash function. </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((MessageType != null ? MessageType.GetHashCode() : 0)*397) ^ (Route != null ? Route.GetHashCode() : 0);
            }
        }
    }
}