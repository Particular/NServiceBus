namespace NServiceBus.Testing
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;

    /// <summary>
    /// A testable implementation for <see cref="IForwardingContext" />.
    /// </summary>
    public class TestableForwardingContext : TestableBehaviorContext, IForwardingContext
    {
        /// <summary>
        /// The message to be forwarded.
        /// </summary>
        public OutgoingMessage Message { get; set; } = new OutgoingMessage(Guid.NewGuid().ToString(), new Dictionary<string, string>(), new byte[0]);

        /// <summary>
        /// The address of the forwarding queue.
        /// </summary>
        public string Address { get; set; } = string.Empty;
    }
}