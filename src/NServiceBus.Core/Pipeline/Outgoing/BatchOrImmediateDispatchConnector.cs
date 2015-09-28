namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using DeliveryConstraints;
    using Pipeline;
    using TransportDispatch;
    using Transports;

    class BatchOrImmediateDispatchConnector : StageConnector<DispatchContext, ImmediateDispatchContext>
    {
        public async override Task Invoke(DispatchContext context, Func<ImmediateDispatchContext, Task> next)
        {
            var state = context.GetOrCreate<State>();
            var dispatchConsistency = state.ImmediateDispatch ? DispatchConsistency.Isolated : DispatchConsistency.Default;

            var options = new DispatchOptions(context.RoutingStrategy, dispatchConsistency, context.GetDeliveryConstraints());
            var operation = new TransportOperation(context.Message, options);


            PendingTransportOperations pendingOperations;

            if (!state.ImmediateDispatch && context.TryGet(out pendingOperations))
            {
                pendingOperations.Add(operation);
                return;
            }

            await next(new ImmediateDispatchContext(new[] { operation }, context)).ConfigureAwait(false);
        }

        public class State
        {
            public bool ImmediateDispatch { get; set; }
        }
    }

    class ForceImmediateDispatchForOperationsInSupressedScopeBehavior : Behavior<DispatchContext>
    {
        public override Task Invoke(DispatchContext context, Func<Task> next)
        {
            var state = context.GetOrCreate<InvokeHandlersBehavior.State>();

            //if there is no scope here the user must have suppressed it
            if (state.ScopeWasPresent && Transaction.Current == null)
            {
                context.GetOrCreate<BatchOrImmediateDispatchConnector.State>()
                    .ImmediateDispatch = true;
            }

            return next();
        }
    }
}