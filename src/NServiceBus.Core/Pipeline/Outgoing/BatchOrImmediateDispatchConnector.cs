namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using DeliveryConstraints;
    using Pipeline;
    using Routing;
    using TransportDispatch;
    using Transports;

    class BatchOrImmediateDispatchConnector : StageConnector<DispatchContext, ImmediateDispatchContext>
    {
        public async override Task Invoke(DispatchContext context, Func<ImmediateDispatchContext, Task> next)
        {
            var options = new DispatchOptions(context.GetRoutingStrategy(), DispatchConsistency.Default, context.GetDeliveryConstraints());
            var operation = new TransportOperation(context.Message, options);

            PendingTransportOperations pendingOperations;

            if (context.TryGet(out pendingOperations))
            {
                pendingOperations.Add(operation);
                return;
            }

            await next(new ImmediateDispatchContext(new[] { operation }, context)).ConfigureAwait(false);
        }
    }
}