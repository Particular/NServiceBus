// ReSharper disable PartialTypeWithSinglePart
namespace NServiceBus.Testing
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Pipeline;
    using Transport;

    /// <summary>
    /// A testable implementation of <see cref="IIncomingPhysicalMessageContext" />.
    /// </summary>
    public partial class TestableIncomingPhysicalMessageContext : TestableIncomingContext, IIncomingPhysicalMessageContext
    {
        /// <summary>
        /// Updates the message with the given body.
        /// </summary>
        public virtual void UpdateMessage(byte[] body)
        {
            Message.Body = body;
        }

        /// <summary>
        /// The physical message being processed.
        /// </summary>
        public IncomingMessage Message { get; set; } = new IncomingMessage(Guid.NewGuid().ToString(), new Dictionary<string, string>(), Stream.Null);
    }
}