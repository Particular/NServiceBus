namespace NServiceBus.Transports
{
    using System.Collections.Generic;
    using NServiceBus.ConsistencyGuarantees;
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
        /// <param name="minimumConsistencyGuarantee">The level of consistency that's required for this operation.</param>
        /// <param name="deliveryConstraints">The delivery constraints that must be honored by the transport.</param>
        /// <param name="context">The pipeline context if present.</param>
        public DispatchOptions(RoutingStrategy routingStrategy, ConsistencyGuarantee minimumConsistencyGuarantee, IEnumerable<DeliveryConstraint> deliveryConstraints, ContextBag context = null)
        {
            RoutingStrategy = routingStrategy;
            MinimumConsistencyGuarantee = minimumConsistencyGuarantee;
            DeliveryConstraints = deliveryConstraints;
            Context = context;

            if (context == null)
            {
                Context = new ContextBag();
            }
        }

        /// <summary>
        /// Creates the send options with the given address.
        /// </summary>
        /// <param name="destination">The destination when the message should go to.</param>
        /// <param name="minimumConsistencyGuarantee">The level of consistency that's required for this operation.</param>
        /// <param name="deliveryConstraints">The delivery constraints that must be honored by the transport.</param>
        /// <param name="context">The pipeline context if present.</param>
        public DispatchOptions(string destination, ConsistencyGuarantee minimumConsistencyGuarantee, IEnumerable<DeliveryConstraint> deliveryConstraints, ContextBag context = null)
            : this(new DirectToTargetDestination(destination), minimumConsistencyGuarantee,deliveryConstraints,context)
        {
          
        }
        /// <summary>
        /// The strategy to use when routing this message.
        /// </summary>
        public RoutingStrategy RoutingStrategy { get; set; }

        /// <summary>
        /// The level of consistency that's required for this operation.
        /// </summary>
        public ConsistencyGuarantee MinimumConsistencyGuarantee { get; private set; }

        /// <summary>
        /// The delivery constraints that must be honored by the transport.
        /// </summary>
        public IEnumerable<DeliveryConstraint> DeliveryConstraints { get; private set; }

        /// <summary>
        /// Access to the behavior context.
        /// </summary>
        public ContextBag Context { get; private set; }
    }
}