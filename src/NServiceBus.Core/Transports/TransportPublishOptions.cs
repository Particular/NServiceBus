namespace NServiceBus.Transports
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.ConsistencyGuarantees;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Pipeline;

    /// <summary>
    /// Contains details on how the message should be published
    /// </summary>
    public class TransportPublishOptions
    {
        /// <summary>
        /// Creates the send options with the given address
        /// </summary>
        /// <param name="eventType">The type of event being published</param>
        /// <param name="minimumConsistencyGuarantee">The level of consistency that's required for this operation</param>
        /// <param name="deliveryConstraints">The delivery constraints that must be honored by the transport</param>
        /// <param name="context">The current pipeline context if one is present</param>
        public TransportPublishOptions(Type eventType, ConsistencyGuarantee minimumConsistencyGuarantee, List<DeliveryConstraint> deliveryConstraints,BehaviorContext context)
        {
            EventType = eventType;
            MinimumConsistencyGuarantee = minimumConsistencyGuarantee;
            DeliveryConstraints = deliveryConstraints;
            Context = context;
        }

        /// <summary>
        /// The type of event being published
        /// </summary>
        public Type EventType { get; private set; }

        /// <summary>
        /// The level of consistency that's required for this operation
        /// </summary>
        public ConsistencyGuarantee MinimumConsistencyGuarantee { get; private set; }

        /// <summary>
        /// The delivery constraints that must be honored by the transport
        /// </summary>
        public IEnumerable<DeliveryConstraint> DeliveryConstraints { get; private set; }

        /// <summary>
        /// Access to the current pipeline context
        /// </summary>
        public BehaviorContext Context { get; private set; }
    }
}