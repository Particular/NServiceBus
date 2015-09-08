namespace NServiceBus.Routing
{
    using System;

    /// <summary>
    /// Represents a route that should deliver the message to all interested subscribers.
    /// </summary>
    public class IndirectAddressLabel:AddressLabel
    {
        /// <summary>
        /// Initializes a new insatnce of <see cref="IndirectAddressLabel"/>.
        /// </summary>
        /// <param name="messageType">The event being published.</param>
        public IndirectAddressLabel(Type messageType)
        {
            MessageType = messageType;
        }

        /// <summary>
        /// The event being published.
        /// </summary>
        public Type MessageType { get; private set; }
    }
}