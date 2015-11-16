namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
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

            var operations = context.RoutingStrategies
                .Select(rs =>
                {
                    var headers = new Dictionary<string, string>(context.Headers);
                    var addressLabel = rs.Apply(headers);
                    var message = new OutgoingMessage(context.MessageId, context.Headers, context.Body);
                    return new TransportOperation(message, new DispatchOptions(addressLabel, dispatchConsistency, context.GetDeliveryConstraints()));
                });            

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