namespace NServiceBus.Routing.MessageDrivenSubscriptions
{
    using System;

    /// <summary>
    /// Represents an entry in a publisher table.
    /// </summary>
    public class PublisherTableEntry
    {
        /// <summary>
        /// Creates a new entry.
        /// </summary>
        public PublisherTableEntry(Type eventType, PublisherAddress address)
        {
            EventType = eventType;
            Address = address;
        }

        /// <summary>
        /// Type of event.
        /// </summary>
        public Type EventType { get; }

        /// <summary>
        /// Addres.
        /// </summary>
        public PublisherAddress Address { get; }
    }
}