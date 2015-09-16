namespace NServiceBus.TransportDispatch
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Transports;

    abstract class DispatchStrategy
    {
        public abstract Task Dispatch(IDispatchMessages dispatcher,OutgoingMessage message,
            RoutingStrategy routingStrategy,
            IEnumerable<DeliveryConstraint> constraints,
            BehaviorContext currentContext,
            DispatchConsistency dispatchConsistency);
    }
}