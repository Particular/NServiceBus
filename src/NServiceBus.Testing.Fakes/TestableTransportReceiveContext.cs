namespace NServiceBus.Testing
{
    using System;
    using System.Collections.Generic;
    using Pipeline;
    using Transport;

    /// <summary>
    /// A testable implementation for <see cref="ITransportReceiveContext" />.
    /// </summary>
    public partial class TestableTransportReceiveContext : TestableBehaviorContext, ITransportReceiveContext
    {
        /// <summary>
        /// The physical message being processed.
        /// </summary>
        public IncomingMessage Message { get; set; } = new IncomingMessage(Guid.NewGuid().ToString(), new Dictionary<string, string>(), new MessageBody(new byte[0]));
    }
}