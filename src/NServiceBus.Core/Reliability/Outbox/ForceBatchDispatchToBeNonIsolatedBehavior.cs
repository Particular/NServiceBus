namespace NServiceBus;

using System;
using System.Threading.Tasks;
using Pipeline;
using Transport;

class ForceBatchDispatchToBeNonIsolatedBehavior : IBehavior<IBatchDispatchContext, IBatchDispatchContext>
{
    public Task Invoke(IBatchDispatchContext context, Func<IBatchDispatchContext, Task> next)
    {
        foreach (var operation in context.Operations)
        {
            // Changing the dispatch consistency to be non-isolated to make sure the transport 
            // enlists the operations in the receive transaction.
            operation.RequiredDispatchConsistency = DispatchConsistency.Default;
        }
        return next(context);
    }
}