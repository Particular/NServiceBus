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
        /// Creates a new instance of <see cref="TestableIncomingPhysicalMessageContext"/>.
        /// </summary>
        public TestableIncomingPhysicalMessageContext()
        {
            Message = new IncomingMessage(Guid.NewGuid().ToString(), new Dictionary<string, string>(), Stream.Null);
        }

        /// <summary>
        /// Updates the message with the given body.
        /// </summary>
        public virtual void UpdateMessage(byte[] body)
        {
            Message = new IncomingMessage(Message.MessageId, Message.Headers, new MemoryStream(body));
        }

        /// <summary>
        /// The physical message being processed.
        /// </summary>
        public IncomingMessage Message { get; set; }
    }
}