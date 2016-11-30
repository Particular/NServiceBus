// ReSharper disable PartialTypeWithSinglePart
namespace NServiceBus.Testing
{
    using System;
    using System.Collections.Generic;
    using Pipeline;
    using Routing;
    using Transport;

    /// <summary>
    /// A testable implementation of <see cref="IUnicastRoutingContext" />.
    /// </summary>
    public partial class TestableUnicastRoutingContext : TestableBehaviorContext, IUnicastRoutingContext
    {
        /// <summary>
        /// The message to dispatch the the transport.
        /// </summary>
        public OutgoingMessage Message { get; set; } = new OutgoingMessage(Guid.NewGuid().ToString(), new Dictionary<string, string>(), new byte[0]);

        /// <summary>
        /// The routing strategies for the operation to be dispatched.
        /// </summary>
        public IReadOnlyCollection<UnicastRoute> Destinations { get; set; } = new List<UnicastRoute>();

        /// <summary>
        /// The function that determines to which instances of the endpoints the message should be distributed.
        /// </summary>
        public Func<string[], string[]> DistributionFunction { get; } = c => c;
    }
}