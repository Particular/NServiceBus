// ReSharper disable PartialTypeWithSinglePart
namespace NServiceBus.Testing
{
    using System;
    using System.Collections.Generic;
    using Pipeline;
    using Transport;

    /// <summary>
    /// A testable implementation for <see cref="IForwardingContext" />.
    /// </summary>
#pragma warning disable 618
    public partial class TestableForwardingContext : TestableBehaviorContext, IForwardingContext
#pragma warning restore 618
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