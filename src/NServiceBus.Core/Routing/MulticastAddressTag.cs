namespace NServiceBus.Routing
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a route that should deliver the message to all interested subscribers.
    /// </summary>
    public class MulticastAddressTag:AddressTag
    {
        /// <summary>
        /// Initializes a new insatnce of <see cref="MulticastAddressTag"/>.
        /// </summary>
        /// <param name="messageType">The event being published.</param>
        /// <param name="extensionData">Extension data.</param>
        public MulticastAddressTag(Type messageType, Dictionary<string, string> extensionData)
            : base(extensionData)
        {
            MessageType = messageType;
        }

        /// <summary>
        /// The event being published.
        /// </summary>
        public Type MessageType { get; private set; }
    }
}