namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;
    using Transport;

    class ForceBatchDispatchToBeIsolatedBehavior : IBehavior<IBatchDispatchContext, IBatchDispatchContext>
    {
        public Task Invoke(IBatchDispatchContext context, Func<IBatchDispatchContext, Task> next)
        {
            foreach (var operation in context.Operations)
            {
                // Changing the dispatch consistency to be isolated to make sure the transport doesn't
                // enlist the operations in the receive transaction. The transport might still want to batch
                // operations for efficiency reasons but should never enlist in the incoming transport transaction.
                // Otherwise a failure to ACK the incoming message after Outbox storage has been set to Dispatched
                // would result in outgoing message loss.
                operation.RequiredDispatchConsistency = DispatchConsistency.Isolated;
            }
            return next(context);
        }
    }
}