namespace NServiceBus.TransportDispatch
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.ConsistencyGuarantees;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Transports;

    class DefaultDispatchStrategy : DispatchStrategy
    {
        
        public override Task Dispatch(IDispatchMessages dispatcher,OutgoingMessage message, RoutingStrategy routingStrategy, ConsistencyGuarantee minimumConsistencyGuarantee, IEnumerable<DeliveryConstraint> constraints, BehaviorContext currentContext)
        {
            return dispatcher.Dispatch(message, new DispatchOptions(routingStrategy, minimumConsistencyGuarantee, constraints, currentContext));
        }
    }
}