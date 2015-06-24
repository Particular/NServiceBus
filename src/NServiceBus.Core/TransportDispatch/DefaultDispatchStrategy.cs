namespace NServiceBus.TransportDispatch
{
    using System.Collections.Generic;
    using NServiceBus.ConsistencyGuarantees;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Transports;

    class DefaultDispatchStrategy : DispatchStrategy
    {
        
        public override void Dispatch(IDispatchMessages dispatcher,OutgoingMessage message, RoutingStrategy routingStrategy, ConsistencyGuarantee minimumConsistencyGuarantee, IEnumerable<DeliveryConstraint> constraints, BehaviorContext currentContext)
        {
            dispatcher.Dispatch(message, new DispatchOptions(routingStrategy, minimumConsistencyGuarantee, constraints, currentContext));
        }
    }
}