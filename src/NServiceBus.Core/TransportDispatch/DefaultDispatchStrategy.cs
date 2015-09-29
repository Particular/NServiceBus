namespace NServiceBus.TransportDispatch
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Extensibility;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Transports;

    class DefaultDispatchStrategy : DispatchStrategy
    {
        public override async Task Dispatch(IDispatchMessages dispatcher, OutgoingMessage message, RoutingStrategy routingStrategy, IEnumerable<DeliveryConstraint> constraints, BehaviorContext currentContext, DispatchConsistency dispatchConsistency)
        {
            var dispatchOptions = new DispatchOptions(routingStrategy, currentContext, constraints, dispatchConsistency);
            await dispatcher.Dispatch(new [] { new TransportOperation(message, dispatchOptions)});
            dispatchOptions.DeliveryConstraints.RaiseErrorIfNotAllConstrainstHaveBeenHandled();
            foreach (var state in currentContext.GetAll<OutgoingPipelineExtensionState>())
            {
                state.ValidateHandled();
            }
        }
    }
}