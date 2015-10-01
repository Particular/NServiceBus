namespace NServiceBus.Transports
{
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Extensibility;
    using NServiceBus.Routing;

    /// <summary>
    /// Contains details on how the message should be sent.
    /// </summary>
    public class DispatchOptions
    {
        /// <summary>
        /// Creates the send options with the given routing strategy.
        /// </summary>
        /// <param name="routingStrategy">The strategy to use when routing this message.</param>
        /// <param name="deliveryConstraints">The delivery constraints that must be honored by the transport.</param>
        /// <param name="context">The pipeline context if present.</param>
        /// <param name="requiredDispatchConsistency">The required consistency level for the dispatch operation.</param>
        public DispatchOptions(RoutingStrategy routingStrategy, ContextBag context, IEnumerable<DeliveryConstraint> deliveryConstraints = null, DispatchConsistency requiredDispatchConsistency = DispatchConsistency.Default)
        {
            RoutingStrategy = routingStrategy;
            DeliveryConstraints = new DeliveryConstraintCollection(deliveryConstraints ?? Enumerable.Empty<DeliveryConstraint>());
            RequiredDispatchConsistency = requiredDispatchConsistency;
            Context = context;
        }

        /// <summary>
        /// The strategy to use when routing this message.
        /// </summary>
        public RoutingStrategy RoutingStrategy { get; set; }

        /// <summary>
        /// The delivery constraints that must be honored by the transport.
        /// </summary>
        public DeliveryConstraintCollection DeliveryConstraints { get; private set; }

        /// <summary>
        /// The dispatch consistency the must be honored by the transport.
        /// </summary>
        public DispatchConsistency RequiredDispatchConsistency { get; private set; }

        /// <summary>
        /// Access to the behavior context.
        /// </summary>
        public ReadOnlyContextBag Context { get; private set; }
    }
}