namespace NServiceBus
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using DeliveryConstraints;
    using Pipeline;
    using TransportDispatch;
    using Transports;

    class RoutingToDispatchConnector : StageConnector<RoutingContext, DispatchContext>
    {
        public async override Task Invoke(RoutingContext context, Func<DispatchContext, Task> next)
        {
            var state = context.GetOrCreate<State>();
            var dispatchConsistency = state.ImmediateDispatch ? DispatchConsistency.Isolated : DispatchConsistency.Default;

            var operations = context.AddressLabels
                .Select(l => new TransportOperation(context.Message, new DispatchOptions(l, dispatchConsistency, context.GetDeliveryConstraints())));            

            PendingTransportOperations pendingOperations;

            if (!state.ImmediateDispatch && context.TryGet(out pendingOperations))
            {
                pendingOperations.AddRange(operations);
                return;
            }

            await next(new DispatchContext(operations.ToArray(), context)).ConfigureAwait(false);
        }

        public class State
        {
            public bool ImmediateDispatch { get; set; }
        }
    }
}