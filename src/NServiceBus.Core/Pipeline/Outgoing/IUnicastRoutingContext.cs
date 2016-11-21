namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using Routing;
    using Transport;

    /// <summary>
    /// An input context for the unicast routing pipe.
    /// </summary>
    public interface IUnicastRoutingContext : IBehaviorContext
    {
        /// <summary>
        /// The message to dispatch the the transport.
        /// </summary>
        OutgoingMessage Message { get; }

        /// <summary>
        /// The routes for the operation to be dispatched.
        /// </summary>
        IReadOnlyCollection<UnicastRoute> Destinations { get; }

        /// <summary>
        /// The function that determines to which instances of the endpoints the message should be distributed.
        /// </summary>
        Func<string[], string[]> DistributionFunction { get; }
    }
}