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
            // Making sure that dispatch consistency is non-isolated to make sure the transport 
            // enlists the outgoing operations in the current receive transaction.
            operation.RequiredDispatchConsistency = DispatchConsistency.Default;
        }
        return next(context);
    }
}