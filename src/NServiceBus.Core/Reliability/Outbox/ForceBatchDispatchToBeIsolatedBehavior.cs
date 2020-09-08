namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Pipeline;
    using Transport;

    class ForceBatchDispatchToBeIsolatedBehavior : IBehavior<IBatchDispatchContext, IBatchDispatchContext>
    {
        public Task Invoke(IBatchDispatchContext context, Func<IBatchDispatchContext, CancellationToken, Task> next, CancellationToken cancellationToken)
        {
            foreach (var operation in context.Operations)
            {
                operation.RequiredDispatchConsistency = DispatchConsistency.Isolated;
            }
            return next(context, cancellationToken);
        }
    }
}