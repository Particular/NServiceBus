namespace NServiceBus.Testing
{
    using System;
    using System.Collections.Generic;
    using Pipeline;
    using Routing;
    using Transport;

    /// <summary>
    /// A testable implementation of <see cref="IRoutingContext" />.
    /// </summary>
    public partial class TestableRoutingContext : TestableBehaviorContext, IRoutingContext
    {
        /// <summary>
        /// The message to dispatch the the transport.
        /// </summary>
        public OutgoingMessage Message { get; set; } = new OutgoingMessage(Guid.NewGuid().ToString(), new Dictionary<string, string>(), new byte[0]);

        /// <summary>
        /// The routing strategies for the operation to be dispatched.
        /// </summary>
        public IReadOnlyCollection<RoutingStrategy> RoutingStrategies { get; set; } = new RoutingStrategy[0];
    }
}